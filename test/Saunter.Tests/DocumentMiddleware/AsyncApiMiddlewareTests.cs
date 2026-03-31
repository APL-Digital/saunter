using System.IO;
using System.Text;
using System.Threading.Tasks;
using ByteBard.AsyncAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Saunter.DocumentMiddleware;
using Saunter.Options;
using Shouldly;
using Xunit;

namespace Saunter.Tests.DocumentMiddleware
{
    public class AsyncApiMiddlewareTests
    {
        [Fact]
        public async Task Invoke_WritesAsyncApiV3Document()
        {
            var options = Options.Create(new AsyncApiOptions());
            options.Value.Middleware.Route = "/asyncapi/asyncapi.json";

            var middleware = new AsyncApiMiddleware(
                _ => Task.CompletedTask,
                options,
                new TestDocumentProvider());

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/asyncapi/asyncapi.json";
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
            context.Response.Body.Position = 0;

            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            body.ShouldContain("\"asyncapi\":\"3.0.0\"");
            body.ShouldContain("\"channels\"");
            body.ShouldContain("\"operations\"");
        }

        private class TestDocumentProvider : IAsyncApiDocumentProvider
        {
            public AsyncApiDocument GetDocument(string? documentName, AsyncApiOptions options)
            {
                return new AsyncApiDocument
                {
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfo
                    {
                        Title = "test",
                        Version = "1.0.0"
                    },
                    Channels =
                    {
                        ["channel"] = new AsyncApiChannel
                        {
                            Address = "channel"
                        }
                    },
                    Operations =
                    {
                        ["operation"] = new AsyncApiOperation
                        {
                            Action = AsyncApiAction.Send,
                            Channel = new AsyncApiChannelReference("#/channels/channel")
                        }
                    }
                };
            }
        }
    }
}
