using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.Options;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.DocumentMiddleware
{
    internal class AsyncApiMiddleware
    {
        private const string DefaultDocumentCacheKey = "__default";

        private readonly RequestDelegate _next;
        private readonly AsyncApiOptions _options;
        private readonly ConcurrentDictionary<string, string> _documentJsonCache = new();

        public AsyncApiMiddleware(RequestDelegate next, IOptions<AsyncApiOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            var isYaml = false;
            if (!IsRequestingAsyncApiSchema(context.Request))
            {
                isYaml = IsRequestingAsyncApiSchemaYaml(context.Request);
                if (!isYaml)
                {
                    await _next(context);
                    return;
                }
            }

            if (context.TryGetDocument(out var documentName) && !_options.NamedApis.TryGetValue(documentName, out _))
            {
                await _next(context);
                return;
            }

            var cacheKey = (documentName ?? DefaultDocumentCacheKey) + (isYaml ? ":yaml" : string.Empty);
            var asyncApiSchema = _documentJsonCache.GetOrAdd(cacheKey, _ =>
            {
                // Resolve per request so scope-registered document providers and user
                // filters bind to the request scope rather than the root container.
                var documentProvider = context.RequestServices.GetRequiredService<IAsyncApiDocumentProvider>();
                var documentWriter = context.RequestServices.GetRequiredService<IAsyncApiDocumentWriter>();
                var document = documentProvider.GetDocument(documentName, _options);
                return isYaml ? documentWriter.WriteYaml(document) : documentWriter.WriteJson(document);
            });

            await RespondWithAsyncApiSchema(context.Response, asyncApiSchema, isYaml ? "application/yaml" : "application/json");
        }

        private static async Task RespondWithAsyncApiSchema(HttpResponse response, string asyncApiSchema, string contentType)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = contentType;

            await response.WriteAsync(asyncApiSchema);
        }

        private bool IsRequestingAsyncApiSchema(HttpRequest request)
        {
            return HttpMethods.IsGet(request.Method) && request.Path.IsMatchingRoute(_options.Middleware.Route);
        }

        private bool IsRequestingAsyncApiSchemaYaml(HttpRequest request)
        {
            var yamlRoute = AsyncApiEndpointRouteBuilderExtensions.DeriveYamlRoute(_options.Middleware.Route);
            return yamlRoute is not null && HttpMethods.IsGet(request.Method) && request.Path.IsMatchingRoute(yamlRoute);
        }
    }
}
