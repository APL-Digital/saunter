using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Saunter.DocumentMiddleware;
using Saunter.Options;

namespace Saunter.UI
{
    internal class AsyncApiUiMiddleware
    {
        private readonly AsyncApiOptions _options;

        public AsyncApiUiMiddleware(RequestDelegate next, IOptions<AsyncApiOptions> options)
        {
            _ = next;
            _options = options.Value;
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
                var hasDocument = context.TryGetDocument(out var document);
                var documentUrl = hasDocument
                    ? GetDocumentFullRoute(context.Request).Replace("{document}", document)
                    : GetDocumentFullRoute(context.Request);
                var uiBaseRoute = hasDocument
                    ? GetUiBaseFullRoute(context.Request).Replace("{document}", document)
                    : GetUiBaseFullRoute(context.Request);

                await AsyncApiUiResources.RespondWithHtml(
                    context.Response,
                    _options.Middleware.UiTitle,
                    documentUrl,
                    $"{uiBaseRoute}/default.min.css",
                    $"{uiBaseRoute}/index.js");
                return;
            }

            if (!TryGetRequestedAssetPath(context, out var assetPath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }
            else
            {
                await AsyncApiUiResources.RespondWithEmbeddedAsset(context.Response, assetPath);
            }
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

        private string GetUiBaseFullRoute(HttpRequest request)
        {
            if (request.PathBase != null)
            {
                return request.PathBase.Add(UiBaseRoute);
            }

            return UiBaseRoute;
        }

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
