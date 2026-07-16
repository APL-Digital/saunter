#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Saunter.AttributeProvider.Attributes;
using Saunter.Options;
using Shouldly;
using Xunit;

namespace Saunter.Tests
{
    public class DefaultsTests
    {
        [Fact]
        public void GetEffectiveScanAssemblies_FallsBackToEntryAssembly_WhenMarkerTypesEmpty()
        {
            var options = new AsyncApiOptions();

            var assemblies = options.GetEffectiveScanAssemblies();

            assemblies.ShouldBe(new[] { Assembly.GetEntryAssembly() });
        }

        [Fact]
        public void GetEffectiveScanAssemblies_UsesMarkerTypeAssemblies_WhenSet()
        {
            var options = new AsyncApiOptions
            {
                AssemblyMarkerTypes = new[] { typeof(DefaultsTests), typeof(AsyncApiOptions) },
            };

            var assemblies = options.GetEffectiveScanAssemblies();

            assemblies.ShouldBe(new[] { typeof(DefaultsTests).Assembly, typeof(AsyncApiOptions).Assembly });
        }

        [Fact]
        public void AsyncApiDocumentDescriptor_DefaultsAsyncapiTo300()
        {
            new AsyncApiDocumentDescriptor().Asyncapi.ShouldBe("3.0.0");
        }

        [Fact]
        public void GetDocument_DefaultsInfoFromScanAssembly_WhenInfoUnset()
        {
            var provider = BuildProvider(options =>
            {
                options.AssemblyMarkerTypes = new[] { typeof(AnnotatedApi) };
            }, out var options);

            var document = provider.GetDocument("defaults-tests", options);

            document.Info.ShouldNotBeNull();
            document.Info.Title.ShouldBe(typeof(AnnotatedApi).Assembly.GetName().Name);
            document.Info.Version.ShouldNotBeNullOrWhiteSpace();
            document.Info.Version.ShouldNotContain("+");
        }

        [Fact]
        public void GetDocument_DoesNotOverrideExplicitInfo()
        {
            var provider = BuildProvider(options =>
            {
                options.AssemblyMarkerTypes = new[] { typeof(AnnotatedApi) };
                options.AsyncApi.Info = new AsyncApiInfoDescriptor
                {
                    Title = "Explicit Title",
                    Version = "9.9.9",
                };
            }, out var options);

            var document = provider.GetDocument("defaults-tests", options);

            document.Info.Title.ShouldBe("Explicit Title");
            document.Info.Version.ShouldBe("9.9.9");
        }

        [Fact]
        public void GetDocument_DefaultsInfoFromRegistrationMarkerAssembly()
        {
            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration();
            services.ConfigureAsyncApiDocument("v1", registration =>
            {
                registration.MarkerTypes.Add(typeof(AnnotatedNamedApi));
                registration.AttributeDocumentName = "v1";
            });

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AsyncApiOptions>>().Value;
            var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();

            var document = provider.GetDocument("v1", options);

            document.Info.Title.ShouldBe(typeof(AnnotatedNamedApi).Assembly.GetName().Name);
        }

        private static IAsyncApiDocumentProvider BuildProvider(Action<AsyncApiOptions> configure, out AsyncApiOptions options)
        {
            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(configure);

            var serviceProvider = services.BuildServiceProvider();
            options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AsyncApiOptions>>().Value;
            return serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
        }

        [AsyncApi("defaults-tests")]
        private class AnnotatedApi
        {
            [Channel("orders.defaults-tests")]
            [SendOperation(OperationId = "defaultsTestsPublish")]
            public void Publish(string message) { _ = message; }
        }

        [AsyncApi("v1")]
        private class AnnotatedNamedApi
        {
            [Channel("orders.defaults-tests-named")]
            [SendOperation(OperationId = "defaultsTestsNamedPublish")]
            public void Publish(string message) { _ = message; }
        }
    }

    public class ServerDescriptorFactoryTests
    {
        [Theory]
        [InlineData("rabbitmq://guest:guest@localhost:5672/", "amqp", "localhost:5672", null)]
        [InlineData("amqp://broker.internal", "amqp", "broker.internal", null)]
        [InlineData("amqps://broker.internal:5671/vhost", "amqps", "broker.internal:5671", "/vhost")]
        [InlineData("kafka://kafka.internal:9092", "kafka", "kafka.internal:9092", null)]
        public void FromUri_MapsSchemeHostAndPath(string uri, string expectedProtocol, string expectedHost, string? expectedPathName)
        {
            var descriptor = AsyncApiServerDescriptor.FromUri(new Uri(uri));

            descriptor.Protocol.ShouldBe(expectedProtocol);
            descriptor.Host.ShouldBe(expectedHost);
            descriptor.PathName.ShouldBe(expectedPathName);
        }

        [Fact]
        public void FromUri_ProtocolOverrideWins()
        {
            var descriptor = AsyncApiServerDescriptor.FromUri(new Uri("rabbitmq://localhost"), "amqp-custom");

            descriptor.Protocol.ShouldBe("amqp-custom");
        }

        [Fact]
        public void FromConnectionString_ParsesUri()
        {
            var descriptor = AsyncApiServerDescriptor.FromConnectionString("amqp://localhost:5672");

            descriptor.Protocol.ShouldBe("amqp");
            descriptor.Host.ShouldBe("localhost:5672");
        }

        [Fact]
        public void FromConnectionString_ThrowsForInvalidUri()
        {
            Should.Throw<ArgumentException>(() => AsyncApiServerDescriptor.FromConnectionString("not a uri"));
        }
    }
}
