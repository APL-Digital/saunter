using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Saunter.Options;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.DocumentMiddleware
{
    internal class AsyncApiMiddleware
    {
        private const string DefaultDocumentCacheKey = "__default";

        private readonly RequestDelegate _next;
        private readonly IAsyncApiDocumentProvider _asyncApiDocumentProvider;
        private readonly IAsyncApiDocumentWriter _documentWriter;
        private readonly AsyncApiOptions _options;
        private readonly ConcurrentDictionary<string, string> _documentJsonCache = new();

        public AsyncApiMiddleware(RequestDelegate next, IOptions<AsyncApiOptions> options, IAsyncApiDocumentProvider asyncApiDocumentProvider, IAsyncApiDocumentWriter documentWriter)
        {
            _next = next;
            _asyncApiDocumentProvider = asyncApiDocumentProvider;
            _documentWriter = documentWriter;
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
                var asyncApiSchema = _asyncApiDocumentProvider.GetDocument(documentName, _options);
                return _documentWriter.WriteJson(asyncApiSchema);
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
