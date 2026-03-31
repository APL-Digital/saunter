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
        private readonly RequestDelegate _next;
        private readonly IAsyncApiDocumentProvider _asyncApiDocumentProvider;
        private readonly IAsyncApiDocumentWriter _documentWriter;
        private readonly AsyncApiOptions _options;

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

            var asyncApiSchema = _asyncApiDocumentProvider.GetDocument(documentName, _options);
            await RespondWithAsyncApiSchemaJson(context.Response, asyncApiSchema, _documentWriter);
        }

        private static async Task RespondWithAsyncApiSchemaJson(HttpResponse response, AsyncApiDocumentDescriptor asyncApiSchema, IAsyncApiDocumentWriter documentWriter)
        {
            var asyncApiSchemaJson = documentWriter.WriteJson(asyncApiSchema);
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
