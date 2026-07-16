using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
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
            routes.ShouldContain("/asyncapi/orders/asyncapi.yaml");
            routes.ShouldContain("/asyncapi/orders/ui");
            routes.ShouldContain("/asyncapi/orders/ui/index.html");
            routes.ShouldContain("/asyncapi/orders/ui/{assetName}");
            routes.ShouldNotContain("/asyncapi/{document}/asyncapi.json");
        }

        [Fact]
        public void MapAsyncApi_MapsDocumentsAndUi()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddAsyncApiSchemaGeneration();
            builder.Services.ConfigureAsyncApiDocument("orders", document =>
            {
                document.AttributeDocumentName = "v1";
            });

            using var app = builder.Build();

            app.MapAsyncApi();

            var routes = ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .Select(endpoint => endpoint.RoutePattern.RawText)
                .ToArray();

            routes.ShouldContain("/asyncapi/orders/asyncapi.json");
            routes.ShouldContain("/asyncapi/orders/asyncapi.yaml");
            routes.ShouldContain("/asyncapi/orders/ui");
            routes.ShouldContain("/asyncapi/orders/ui/index.html");
            routes.ShouldContain("/asyncapi/orders/ui/{assetName}");
        }

        [Fact]
        public void MapAsyncApiDocuments_LogsMappedRoutes()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddFakeLogging();
            builder.Services.AddAsyncApiSchemaGeneration();
            builder.Services.ConfigureAsyncApiDocument("orders", document =>
            {
                document.AttributeDocumentName = "v1";
            });

            using var app = builder.Build();

            app.MapAsyncApiDocuments();

            var collector = app.Services.GetRequiredService<FakeLogCollector>();
            collector.GetSnapshot().ShouldContain(record =>
                record.Message.Contains("AsyncAPI document 'orders' mapped at /asyncapi/orders/asyncapi.json"));
        }
    }
}
