using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Saunter.Options;
using Saunter.UI;
using Shouldly;
using Xunit;

namespace Saunter.Tests.UI
{
    public class AsyncApiUiMiddlewareTests
    {
        [Fact]
        public async Task Invoke_ServesEmbeddedIndexJsForTemplatedDocumentRoute()
        {
            var middleware = CreateMiddleware();
            var context = CreateAssetRequestContext("index.js");

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
            context.Response.ContentType.ShouldBe("text/javascript");

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            body.ShouldContain("AsyncApiStandalone");
        }

        [Fact]
        public async Task Invoke_ServesEmbeddedCssForTemplatedDocumentRoute()
        {
            var middleware = CreateMiddleware();
            var context = CreateAssetRequestContext("default.min.css");

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
            context.Response.ContentType.ShouldBe("text/css");
            context.Response.Body.Length.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task Invoke_ReturnsNotFoundForUnknownEmbeddedAsset()
        {
            var middleware = CreateMiddleware();
            var context = CreateAssetRequestContext("missing.js");

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task Invoke_ServesHtmlWithAbsoluteDocumentAndAssetUrls()
        {
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/asyncapi/default/ui/index.html";
            context.Request.RouteValues["document"] = "default";
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
            context.Response.Body.Position = 0;

            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            body.ShouldContain("/asyncapi/default/asyncapi.json");
            body.ShouldContain("/asyncapi/default/ui/default.min.css");
            body.ShouldContain("/asyncapi/default/ui/index.js");
        }

        private static AsyncApiUiMiddleware CreateMiddleware()
        {
            var options = Microsoft.Extensions.Options.Options.Create(new AsyncApiOptions());
            options.Value.Middleware.Route = "/asyncapi/{document}/asyncapi.json";
            options.Value.Middleware.UiBaseRoute = "/asyncapi/{document}/ui/";

            return new AsyncApiUiMiddleware(_ => Task.CompletedTask, options);
        }

        private static DefaultHttpContext CreateAssetRequestContext(string assetName)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = $"/asyncapi/default/ui/{assetName}";
            context.Request.RouteValues["document"] = "default";
            context.Request.RouteValues["wildcard"] = assetName;
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}
