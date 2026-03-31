#nullable enable
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Saunter.AttributeProvider.Descriptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Saunter.DocumentMiddleware;
using Saunter.Options;
using Saunter.SharedKernel;
using Shouldly;
using Xunit;

namespace Saunter.Tests.DocumentMiddleware
{
    public class AsyncApiMiddlewareTests
    {
        [Fact]
        public async Task Invoke_WritesAsyncApiV3Document()
        {
            var options = Microsoft.Extensions.Options.Options.Create(new AsyncApiOptions());
            options.Value.Middleware.Route = "/asyncapi/asyncapi.json";

            var middleware = new AsyncApiMiddleware(
                _ => Task.CompletedTask,
                options,
                new TestDocumentProvider(),
                new AsyncApiDocumentWriter());

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/asyncapi/asyncapi.json";
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
            context.Response.Body.Position = 0;

            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            using var json = JsonDocument.Parse(body);

            json.RootElement.GetProperty("asyncapi").GetString().ShouldBe("3.0.0");
            json.RootElement.TryGetProperty("channels", out _).ShouldBeTrue();
            json.RootElement.TryGetProperty("operations", out _).ShouldBeTrue();
        }

        private class TestDocumentProvider : IAsyncApiDocumentProvider
        {
            public AsyncApiDocumentDescriptor GetDocument(string? documentName, AsyncApiOptions options)
            {
                return new AsyncApiDocumentDescriptor
                {
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = "test",
                        Version = "1.0.0"
                    },
                    Channels =
                    {
                        ["channel"] = new AsyncApiChannelDescriptor("channel", "channel", null, null, null, null, [], [], [])
                    },
                    Operations =
                    {
                        ["operation"] = new AsyncApiOperationDescriptor(ByteBard.AsyncAPI.Models.AsyncApiAction.Send, "channel", null, null, null, null, [], [], null)
                    }
                };
            }
        }
    }
}
