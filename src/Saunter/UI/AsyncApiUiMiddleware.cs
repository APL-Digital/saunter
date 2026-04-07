using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Saunter.DocumentMiddleware;
using Saunter.Options;

namespace Saunter.UI
{
    internal class AsyncApiUiMiddleware
    {
        private readonly AsyncApiOptions _options;
        private readonly IFileProvider _fileProvider;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

        public AsyncApiUiMiddleware(RequestDelegate next, IOptions<AsyncApiOptions> options)
        {
            _ = next;
            _options = options.Value;
            _fileProvider = new EmbeddedFileProvider(GetType().Assembly, GetType().Namespace);
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsRequestingUiBase(context.Request))
            {
                context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;

                if (context.TryGetDocument(out var document))
                {
                    context.Response.Headers["Location"] = GetUiIndexFullRoute(context.Request).Replace("{document}", document);
                }
                else
                {
                    context.Response.Headers["Location"] = GetUiIndexFullRoute(context.Request);
                }
                return;
            }

            if (IsRequestingAsyncApiUi(context.Request))
            {
                if (context.TryGetDocument(out var document))
                {
                    await RespondWithAsyncApiHtml(context.Response, GetDocumentFullRoute(context.Request).Replace("{document}", document));
                }
                else
                {
                    await RespondWithAsyncApiHtml(context.Response, GetDocumentFullRoute(context.Request));
                }
                return;
            }

            if (!TryGetRequestedAssetPath(context, out var assetPath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }
            else
            {
                await RespondWithEmbeddedAsset(context.Response, assetPath);
            }
        }

        private async Task RespondWithAsyncApiHtml(HttpResponse response, string route)
        {
            var name = $"{GetType().Namespace}.index.html";

            using var stream = GetType().Assembly.GetManifestResourceStream(name)
                ?? throw new FileNotFoundException(
                    $"Embedded AsyncAPI UI resource '{name}' was not found in assembly '{GetType().Assembly.GetName().Name}'.");

            using var reader = new StreamReader(stream);

            var indexHtml = new StringBuilder(await reader.ReadToEndAsync());

            // Replace dynamic content such as the AsyncAPI document url
            foreach (var replacement in new Dictionary<string, string>
            {
                ["{{title}}"] = _options.Middleware.UiTitle,
                ["{{asyncApiDocumentUrl}}"] = route,
            })
            {
                indexHtml.Replace(replacement.Key, replacement.Value);
            }

            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = MediaTypeNames.Text.Html;
            await response.WriteAsync(indexHtml.ToString(), Encoding.UTF8);
        }

        private async Task RespondWithEmbeddedAsset(HttpResponse response, string assetPath)
        {
            var file = _fileProvider.GetFileInfo(assetPath);
            if (!file.Exists)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (!_contentTypeProvider.TryGetContentType(assetPath, out var contentType))
            {
                contentType = MediaTypeNames.Application.Octet;
            }

            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = contentType;
            response.ContentLength = file.Length;

            await using var stream = file.CreateReadStream();
            await stream.CopyToAsync(response.Body);
        }

        private bool IsRequestingUiBase(HttpRequest request)
        {
            return HttpMethods.IsGet(request.Method) && request.Path.IsMatchingRoute(UiBaseRoute);
        }

        private bool IsRequestingAsyncApiUi(HttpRequest request)
        {
            return HttpMethods.IsGet(request.Method) && request.Path.IsMatchingRoute(UiIndexRoute);
        }

        private static bool TryGetRequestedAssetPath(HttpContext context, out string assetPath)
        {
            if (!context.Request.RouteValues.TryGetValue("wildcard", out var wildcardValue)
                || wildcardValue is not string wildcard
                || string.IsNullOrWhiteSpace(wildcard))
            {
                assetPath = string.Empty;
                return false;
            }

            assetPath = wildcard.Replace('\\', '/').TrimStart('/');
            if (assetPath.Contains(".."))
            {
                assetPath = string.Empty;
                return false;
            }

            return true;
        }

        private string UiIndexRoute => _options.Middleware.UiBaseRoute?.TrimEnd('/') + "/index.html";

        private string GetUiIndexFullRoute(HttpRequest request)
        {
            if (request.PathBase != null)
            {
                return request.PathBase.Add(UiIndexRoute);
            }

            return UiIndexRoute;
        }

        private string UiBaseRoute => _options.Middleware.UiBaseRoute?.TrimEnd('/') ?? string.Empty;

        private string GetDocumentFullRoute(HttpRequest request)
        {
            if (request.PathBase != null)
            {
                return request.PathBase.Add(_options.Middleware.Route);
            }

            return _options.Middleware.Route;
        }
    }
}
