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
            if (!IsRequestingAsyncApiSchema(context.Request))
            {
                await _next(context);
                return;
            }

            if (context.TryGetDocument(out var documentName) && !_options.NamedApis.TryGetValue(documentName, out _))
            {
                await _next(context);
                return;
            }

            var cacheKey = documentName ?? DefaultDocumentCacheKey;
            var asyncApiSchemaJson = _documentJsonCache.GetOrAdd(cacheKey, _ =>
            {
                // Resolve per request so scope-registered document providers and user
                // filters bind to the request scope rather than the root container.
                var documentProvider = context.RequestServices.GetRequiredService<IAsyncApiDocumentProvider>();
                var documentWriter = context.RequestServices.GetRequiredService<IAsyncApiDocumentWriter>();
                var asyncApiSchema = documentProvider.GetDocument(documentName, _options);
                return documentWriter.WriteJson(asyncApiSchema);
            });

            await RespondWithAsyncApiSchemaJson(context.Response, asyncApiSchemaJson);
        }

        private static async Task RespondWithAsyncApiSchemaJson(HttpResponse response, string asyncApiSchemaJson)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "application/json";

            await response.WriteAsync(asyncApiSchemaJson);
        }

        private bool IsRequestingAsyncApiSchema(HttpRequest request)
        {
            return HttpMethods.IsGet(request.Method) && request.Path.IsMatchingRoute(_options.Middleware.Route);
        }
    }
}
