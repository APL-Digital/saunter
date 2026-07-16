#nullable enable
using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.AttributeProvider.Attributes;
using Saunter.Options;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider
{
    public class MassTransitDiscoveryTests
    {
        [Fact]
        public void Discovery_IsOffByDefault()
        {
            var (provider, options) = Arrange(configureDiscovery: null);

            var document = provider.GetDocument("mt-discovery", options);

            document.Operations.Keys.ShouldNotContain("DiscoveredOrderConsumer.DiscoveredOrderSubmitted.receive");
        }

        [Fact]
        public void Discovery_DocumentsUnannotatedConsumers()
        {
            var (provider, options) = Arrange(discovery => discovery.DiscoverMassTransitConsumers = true);

            var document = provider.GetDocument("mt-discovery", options);

            document.Operations.ShouldContainKey("DiscoveredOrderConsumer.DiscoveredOrderSubmitted.receive");
            var operation = document.Operations["DiscoveredOrderConsumer.DiscoveredOrderSubmitted.receive"];
            operation.Action.ShouldBe(ByteBard.AsyncAPI.Models.AsyncApiAction.Receive);
            document.Channels.Values.ShouldContain(channel =>
                channel.Address == typeof(DiscoveredOrderSubmitted).Namespace + ":DiscoveredOrderSubmitted");
        }

        [Fact]
        public void Discovery_DocumentsOneOperationPerConsumedMessageType()
        {
            var (provider, options) = Arrange(discovery => discovery.DiscoverMassTransitConsumers = true);

            var document = provider.GetDocument("mt-discovery", options);

            document.Operations.ShouldContainKey("MultiContractConsumer.DiscoveredOrderCancelled.receive");
            document.Operations.ShouldContainKey("MultiContractConsumer.DiscoveredOrderRefunded.receive");
        }

        [Fact]
        public void Discovery_SkipsAnnotatedConsumers()
        {
            var (provider, options) = Arrange(discovery => discovery.DiscoverMassTransitConsumers = true);

            var document = provider.GetDocument("mt-discovery", options);

            // The annotated consumer keeps its hand-authored operation and no synthesized twin appears.
            document.Operations.ShouldContainKey("annotatedDiscoveryConsume");
            document.Operations.Keys.ShouldNotContain("AnnotatedDiscoveryConsumer.DiscoveredOrderShipped.receive");
        }

        [Fact]
        public void Discovery_RespectsConsumerFilter()
        {
            var (provider, options) = Arrange(discovery =>
            {
                discovery.DiscoverMassTransitConsumers = true;
                discovery.MassTransitConsumerFilter = type => type.AsType() != typeof(DiscoveredOrderConsumer);
            });

            var document = provider.GetDocument("mt-discovery", options);

            document.Operations.Keys.ShouldNotContain("DiscoveredOrderConsumer.DiscoveredOrderSubmitted.receive");
            document.Operations.ShouldContainKey("MultiContractConsumer.DiscoveredOrderCancelled.receive");
        }

        [Fact]
        public void Discovery_RespectsCustomAddressGenerator()
        {
            var (provider, options) = Arrange(discovery =>
            {
                discovery.DiscoverMassTransitConsumers = true;
                discovery.MassTransitChannelAddressGenerator = AsyncApiDiscoveryOptions.KebabCaseEndpointAddress;
            });

            var document = provider.GetDocument("mt-discovery", options);

            document.Channels.Values.ShouldContain(channel => channel.Address == "discovered-order-submitted");
        }

        [Theory]
        [InlineData(typeof(DiscoveredOrderSubmitted), "discovered-order-submitted")]
        [InlineData(typeof(HTTPRequest), "http-request")]
        public void KebabCaseEndpointAddress_KebabCasesTypeNames(Type messageType, string expected)
        {
            AsyncApiDiscoveryOptions.KebabCaseEndpointAddress(messageType).ShouldBe(expected);
        }

        [Fact]
        public void MessageUrnAddress_UsesNamespaceColonTypeName()
        {
            AsyncApiDiscoveryOptions.MessageUrnAddress(typeof(DiscoveredOrderSubmitted))
                .ShouldBe(typeof(DiscoveredOrderSubmitted).Namespace + ":DiscoveredOrderSubmitted");
        }

        private static (IAsyncApiDocumentProvider Provider, AsyncApiOptions Options) Arrange(Action<AsyncApiDiscoveryOptions>? configureDiscovery)
        {
            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration();
            services.ConfigureAsyncApiDocument("mt-discovery", registration =>
            {
                registration.MarkerTypes.Add(typeof(MassTransitDiscoveryTests));
                registration.AttributeDocumentName = "mt-discovery";
                registration.TypeFilter = type => type.DeclaringType == typeof(MassTransitDiscoveryTests);
                registration.Document.Info = new AsyncApiInfoDescriptor { Title = "Discovery", Version = "1.0.0" };
            });

            if (configureDiscovery is not null)
            {
                services.Configure<AsyncApiOptions>(options => configureDiscovery(options.Discovery));
            }

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<AsyncApiOptions>>().Value;
            var provider = serviceProvider.GetRequiredService<IAsyncApiDocumentProvider>();
            return (provider, options);
        }

        public class DiscoveredOrderSubmitted
        {
            public Guid OrderId { get; set; }
        }

        public class DiscoveredOrderCancelled
        {
            public Guid OrderId { get; set; }
        }

        public class DiscoveredOrderRefunded
        {
            public Guid OrderId { get; set; }
        }

        public class DiscoveredOrderShipped
        {
            public Guid OrderId { get; set; }
        }

        public class HTTPRequest
        {
        }

        public class DiscoveredOrderConsumer : IConsumer<DiscoveredOrderSubmitted>
        {
            public Task Consume(ConsumeContext<DiscoveredOrderSubmitted> context) => Task.CompletedTask;
        }

        public class MultiContractConsumer : IConsumer<DiscoveredOrderCancelled>, IConsumer<DiscoveredOrderRefunded>
        {
            public Task Consume(ConsumeContext<DiscoveredOrderCancelled> context) => Task.CompletedTask;

            public Task Consume(ConsumeContext<DiscoveredOrderRefunded> context) => Task.CompletedTask;
        }

        [AsyncApi("mt-discovery")]
        public class AnnotatedDiscoveryConsumer : IConsumer<DiscoveredOrderShipped>
        {
            [Channel("orders.shipped.annotated")]
            [ReceiveOperation(OperationId = "annotatedDiscoveryConsume")]
            public Task Consume(ConsumeContext<DiscoveredOrderShipped> context) => Task.CompletedTask;
        }
    }
}
