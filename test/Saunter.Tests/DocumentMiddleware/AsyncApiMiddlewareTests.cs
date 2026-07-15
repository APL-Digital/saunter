#nullable enable
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.AttributeProvider.Descriptors;
using Saunter.DocumentMiddleware;
using Saunter.Options;
using Saunter.SharedKernel.Interfaces;
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

            var middleware = new AsyncApiMiddleware(_ => Task.CompletedTask, options);

            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration();
            services.AddSingleton<IAsyncApiDocumentProvider, TestDocumentProvider>();
            using var serviceProvider = services.BuildServiceProvider();

            var context = new DefaultHttpContext { RequestServices = serviceProvider };
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

        [Fact]
        public async Task Invoke_CachesRenderedDocumentJsonAcrossRequests()
        {
            var options = Microsoft.Extensions.Options.Options.Create(new AsyncApiOptions());
            options.Value.Middleware.Route = "/asyncapi/asyncapi.json";

            var provider = new CountingDocumentProvider();
            var writer = new CountingDocumentWriter();
            var middleware = new AsyncApiMiddleware(_ => Task.CompletedTask, options);

            var services = new ServiceCollection();
            services.AddSingleton<IAsyncApiDocumentProvider>(provider);
            services.AddSingleton<IAsyncApiDocumentWriter>(writer);
            using var serviceProvider = services.BuildServiceProvider();

            await InvokeDocumentRequest(middleware, serviceProvider);
            await InvokeDocumentRequest(middleware, serviceProvider);

            provider.CallCount.ShouldBe(1);
            writer.CallCount.ShouldBe(1);
        }

        private static async Task InvokeDocumentRequest(AsyncApiMiddleware middleware, System.IServiceProvider serviceProvider)
        {
            var context = new DefaultHttpContext { RequestServices = serviceProvider };
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/asyncapi/asyncapi.json";
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);
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

        private sealed class CountingDocumentProvider : IAsyncApiDocumentProvider
        {
            public int CallCount { get; private set; }

            public AsyncApiDocumentDescriptor GetDocument(string? documentName, AsyncApiOptions options)
            {
                CallCount++;
                return new TestDocumentProvider().GetDocument(documentName, options);
            }
        }

        private sealed class CountingDocumentWriter : IAsyncApiDocumentWriter
        {
            public int CallCount { get; private set; }

            public string WriteJson(AsyncApiDocumentDescriptor document)
            {
                CallCount++;
                return "{\"asyncapi\":\"3.0.0\"}";
            }
        }
    }
}
