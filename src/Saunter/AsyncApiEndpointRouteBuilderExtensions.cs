using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.DocumentMiddleware;
using Saunter.Options;
using Saunter.SharedKernel.Interfaces;
using Saunter.UI;

namespace Saunter
{
    public static class AsyncApiEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps the AsyncAPI document endpoint
        /// </summary>
        public static IEndpointConventionBuilder MapAsyncApiDocuments(
            this IEndpointRouteBuilder endpoints)
        {
            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<AsyncApiOptions>>();
            if (options.Value.Documents.Count > 0)
            {
                var cache = new ConcurrentDictionary<string, string>();
                var group = endpoints.MapGroup(string.Empty);

                foreach (var registration in options.Value.Documents.Values.OrderBy(document => document.Name, System.StringComparer.Ordinal))
                {
                    var documentRoute = registration.Middleware.Route;
                    group.MapGet(documentRoute, (IAsyncApiDocumentProvider provider, IAsyncApiDocumentWriter writer) =>
                    {
                        var json = cache.GetOrAdd(
                            registration.Name,
                            _ => writer.WriteJson(provider.GetDocument(registration.Name, options.Value)));
                        return Results.Text(json, "application/json");
                    });
                }

                return group;
            }

            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<AsyncApiMiddleware>()
                .Build();

            var route = options.Value.Middleware.Route;

            return endpoints.MapGet(route, pipeline);
        }


        /// <summary>
        /// Maps the AsyncAPI UI endpoint(s)
        /// </summary>
        public static IEndpointConventionBuilder MapAsyncApiUi(this IEndpointRouteBuilder endpoints)
        {
            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<AsyncApiOptions>>();
            if (options.Value.Documents.Count > 0)
            {
                var group = endpoints.MapGroup(string.Empty);

                foreach (var registration in options.Value.Documents.Values.OrderBy(document => document.Name, System.StringComparer.Ordinal))
                {
                    var uiBaseRoute = registration.Middleware.UiBaseRoute?.TrimEnd('/') ?? string.Empty;
                    group.MapGet(uiBaseRoute, (HttpRequest request) => Results.Content(
                        RenderRegistrationUiIndexHtml(request, registration),
                        "text/html"));
                    group.MapGet(uiBaseRoute + "/index.html", (HttpRequest request) => Results.Content(
                        RenderRegistrationUiIndexHtml(request, registration),
                        "text/html"));
                    group.MapGet(uiBaseRoute + "/{assetName}", async (HttpContext context, string assetName) =>
                    {
                        await AsyncApiUiResources.RespondWithEmbeddedAsset(context.Response, assetName);
                    });
                }

                return group;
            }

            var pipeline = endpoints.CreateApplicationBuilder()
                // I don't really understand why...
                // https://github.com/dotnet/aspnetcore/issues/24252#issuecomment-663620294
                .Use((context, next) =>
                {
                    context.SetEndpoint(null);
                    return next();
                })
                .UseMiddleware<AsyncApiUiMiddleware>()
                .Build();

            var route = options.Value.Middleware.UiBaseRoute + "{*wildcard}";

            return endpoints.MapGet(route, pipeline);
        }

        private static string RenderRegistrationUiIndexHtml(HttpRequest request, AsyncApiDocumentRegistration registration)
        {
            var documentUrl = WithPathBase(request, registration.Middleware.Route);
            var uiBaseRoute = WithPathBase(request, registration.Middleware.UiBaseRoute?.TrimEnd('/') ?? string.Empty);
            var title = registration.Middleware.UiTitle;
            return AsyncApiUiResources.RenderHtml(
                title,
                documentUrl,
                uiBaseRoute + "/default.min.css",
                uiBaseRoute + "/index.js");
        }

        private static string WithPathBase(HttpRequest request, string route)
        {
            return request.PathBase != null
                ? request.PathBase.Add(route)
                : route;
        }
    }
}
