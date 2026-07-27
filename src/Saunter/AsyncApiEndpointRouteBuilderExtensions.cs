using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saunter.DocumentMiddleware;
using Saunter.Options;
using Saunter.SharedKernel.Interfaces;
using Saunter.UI;

namespace Saunter
{
    /// <summary>
    /// Extension methods for mapping the AsyncAPI document and UI endpoints on an
    /// <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    public static class AsyncApiEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps the AsyncAPI document endpoint(s) and the AsyncAPI UI in one call. Equivalent to
        /// calling <see cref="MapAsyncApiDocuments"/> followed by
        /// <see cref="MapAsyncApiUi(IEndpointRouteBuilder)"/>.
        /// Call the two methods separately when per-endpoint conventions are needed.
        /// </summary>
        public static IEndpointRouteBuilder MapAsyncApi(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapAsyncApiDocuments();
            endpoints.MapAsyncApiUi();
            return endpoints;
        }

        /// <summary>
        /// Maps the AsyncAPI document endpoint(s). When documents are registered via
        /// <c>ConfigureAsyncApiDocument</c>, one endpoint is mapped per registration on its own route;
        /// otherwise a single endpoint is mapped on the shared middleware route.
        /// </summary>
        public static IEndpointConventionBuilder MapAsyncApiDocuments(
            this IEndpointRouteBuilder endpoints)
        {
            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<AsyncApiOptions>>();
            var logger = CreateLogger(endpoints);
            if (options.Value.Documents.Count > 0)
            {
                var cache = new ConcurrentDictionary<string, string>();
                var group = endpoints.MapGroup(string.Empty);

                foreach (var registration in options.Value.Documents.Values.OrderBy(document => document.Name, System.StringComparer.Ordinal))
                {
                    var documentRoute = registration.Middleware.Route;
                    logger?.LogInformation(
                        "AsyncAPI document '{DocumentName}' mapped at {DocumentRoute} (YAML: {YamlRoute}, UI: {UiRoute})",
                        registration.Name,
                        documentRoute,
                        DeriveYamlRoute(documentRoute) ?? "<none>",
                        registration.Middleware.UiBaseRoute ?? "<none>");
                    group.MapGet(documentRoute, (IAsyncApiDocumentProvider provider, IAsyncApiDocumentWriter writer) =>
                    {
                        var json = cache.GetOrAdd(
                            registration.Name,
                            _ => writer.WriteJson(provider.GetDocument(registration.Name, options.Value)));
                        return Results.Text(json, "application/json");
                    });

                    var yamlRoute = DeriveYamlRoute(documentRoute);
                    if (yamlRoute is not null)
                    {
                        group.MapGet(yamlRoute, (IAsyncApiDocumentProvider provider, IAsyncApiDocumentWriter writer) =>
                        {
                            var yaml = cache.GetOrAdd(
                                registration.Name + ":yaml",
                                _ => writer.WriteYaml(provider.GetDocument(registration.Name, options.Value)));
                            return Results.Text(yaml, "application/yaml");
                        });
                    }
                }

                return group;
            }

            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<AsyncApiMiddleware>()
                .Build();

            var route = options.Value.Middleware.Route;
            logger?.LogInformation(
                "AsyncAPI document mapped at {DocumentRoute} (YAML: {YamlRoute}, UI: {UiRoute})",
                route,
                DeriveYamlRoute(route) ?? "<none>",
                options.Value.Middleware.UiBaseRoute ?? "<none>");

            var jsonEndpoint = endpoints.MapGet(route, pipeline);

            var defaultYamlRoute = DeriveYamlRoute(route);
            if (defaultYamlRoute is not null)
            {
                endpoints.MapGet(defaultYamlRoute, pipeline);
            }

            return jsonEndpoint;
        }

        /// <summary>
        /// Derives the YAML sibling of a JSON document route, e.g.
        /// <c>/asyncapi/asyncapi.json</c> becomes <c>/asyncapi/asyncapi.yaml</c>.
        /// Returns <c>null</c> when the route does not end in <c>.json</c>.
        /// </summary>
        public static string? DeriveYamlRoute(string? jsonRoute)
        {
            if (jsonRoute is null || !jsonRoute.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return jsonRoute.Substring(0, jsonRoute.Length - ".json".Length) + ".yaml";
        }


        /// <summary>
        /// Maps the AsyncAPI UI endpoint(s)
        /// </summary>
        public static IEndpointConventionBuilder MapAsyncApiUi(this IEndpointRouteBuilder endpoints)
        {
            WarnWhenUiAssetsMissing(endpoints);

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

        /// <summary>
        /// Maps the AsyncAPI UI endpoints for a document served from somewhere other than this
        /// library's runtime generation — typically a document generated at build time and served
        /// as static bytes. Unlike <see cref="MapAsyncApiUi(IEndpointRouteBuilder)"/>, this does not
        /// consult <see cref="AsyncApiOptions.Documents"/>, so no document registration is required
        /// and no document endpoint is mapped; the caller keeps serving the document itself.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="uiBaseRoute">
        /// Base route for the UI, e.g. <c>/asyncapi/v1/ui</c>. A trailing slash is ignored.
        /// </param>
        /// <param name="documentUrl">
        /// Route the UI fetches the document from, e.g. <c>/asyncapi/v1/asyncapi.json</c>.
        /// </param>
        /// <param name="title">Page title. Defaults to "AsyncAPI" when null or whitespace.</param>
        public static IEndpointConventionBuilder MapAsyncApiUi(
            this IEndpointRouteBuilder endpoints,
            string uiBaseRoute,
            string documentUrl,
            string? title = null)
        {
            WarnWhenUiAssetsMissing(endpoints);

            var baseRoute = uiBaseRoute?.TrimEnd('/') ?? string.Empty;
            var resolvedTitle = string.IsNullOrWhiteSpace(title) ? "AsyncAPI" : title!;
            var group = endpoints.MapGroup(string.Empty);

            group.MapGet(baseRoute, (HttpRequest request) => Results.Content(
                RenderUiIndexHtml(request, baseRoute, documentUrl, resolvedTitle),
                "text/html"));
            group.MapGet(baseRoute + "/index.html", (HttpRequest request) => Results.Content(
                RenderUiIndexHtml(request, baseRoute, documentUrl, resolvedTitle),
                "text/html"));
            group.MapGet(baseRoute + "/{assetName}", async (HttpContext context, string assetName) =>
            {
                await AsyncApiUiResources.RespondWithEmbeddedAsset(context.Response, assetName);
            });

            return group;
        }

        private static void WarnWhenUiAssetsMissing(IEndpointRouteBuilder endpoints)
        {
            if (!AsyncApiUiResources.HasUiAssets)
            {
                CreateLogger(endpoints)?.LogWarning(
                    "The AsyncAPI UI assets (index.js, default.min.css) are not embedded in the Saunter assembly, " +
                    "so the UI will render an explanatory page instead. This happens when Saunter was built from " +
                    "source without running 'npm install' in src/Saunter.UI first. The document endpoint is unaffected.");
            }
        }

        private static string RenderRegistrationUiIndexHtml(HttpRequest request, AsyncApiDocumentRegistration registration)
        {
            var title = AsyncApiMiddlewareOptions.ResolveUiTitle(registration.Middleware.UiTitle, registration.Document);
            return RenderUiIndexHtml(
                request,
                registration.Middleware.UiBaseRoute?.TrimEnd('/') ?? string.Empty,
                registration.Middleware.Route,
                title);
        }

        private static string RenderUiIndexHtml(HttpRequest request, string uiBaseRoute, string documentRoute, string title)
        {
            var resolvedUiBaseRoute = WithPathBase(request, uiBaseRoute);
            return AsyncApiUiResources.RenderHtml(
                title,
                WithPathBase(request, documentRoute),
                resolvedUiBaseRoute + "/default.min.css",
                resolvedUiBaseRoute + "/index.js");
        }

        private static string WithPathBase(HttpRequest request, string route)
        {
            return request.PathBase != null
                ? request.PathBase.Add(route)
                : route;
        }

        private static ILogger? CreateLogger(IEndpointRouteBuilder endpoints)
        {
            return endpoints.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("Saunter");
        }
    }
}
