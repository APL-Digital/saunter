using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Saunter.Tests
{
    public class AsyncApiEndpointRouteBuilderExtensionsTests
    {
        [Fact]
        public void MapAsyncApiEndpoints_UsesConfiguredDocumentRoutes()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddAsyncApiSchemaGeneration();
            builder.Services.ConfigureAsyncApiDocument("orders", document =>
            {
                document.AttributeDocumentName = "v1";
            });

            using var app = builder.Build();

            app.MapAsyncApiDocuments();
            app.MapAsyncApiUi();

            var routes = ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .Select(endpoint => endpoint.RoutePattern.RawText)
                .Where(route => route != null)
                .ToArray();

            routes.ShouldContain("/asyncapi/orders/asyncapi.json");
            routes.ShouldContain("/asyncapi/orders/ui");
            routes.ShouldContain("/asyncapi/orders/ui/index.html");
            routes.ShouldContain("/asyncapi/orders/ui/{assetName}");
            routes.ShouldNotContain("/asyncapi/{document}/asyncapi.json");
        }
    }
}
