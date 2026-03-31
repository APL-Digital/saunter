using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Saunter.AttributeProvider.Attributes;
using Saunter.Options.Filters;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.DocumentGenerationTests
{
    public class InferenceBehaviorTests
    {
        [Fact]
        public void GenerateDocument_ClassLevelExplicitValuesWinOverInference()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ClassLevelExplicitPublisher));

            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            var channel = document.AssertAndGetChannel("classLevelExplicit", "tenant.class.created");
            var send = document.AssertAndGetOperation("ClassLevelExplicitOperation", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);

            send.ChannelId.ShouldBe(channel.Id);
            document.AssertByMessage(send, "classPayload");
        }

        [Fact]
        public void GenerateDocument_DocumentFilterCanOverrideInferredIds()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(FilterOverriddenInferencePublisher));
            options.AddDocumentFilter<InferenceOverrideDocumentFilter>();

            var document = documentProvider.GetDocument(null, options);

            document.Operations.ShouldNotContainKey("Publish");
            document.Channels.ShouldNotContainKey("tenantCreated");
            document.AssertAndGetOperation("filteredPublish", ByteBard.AsyncAPI.Models.AsyncApiAction.Send).ChannelId.ShouldBe("filteredChannel");
            document.AssertAndGetChannel("filteredChannel", "tenant.created");
        }

        [Fact]
        public void GenerateDocument_UsesCustomOperationIdGenerator()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(OperationIdGeneratorPublisher));
            options.Inference.OperationIdGenerator = (member, action) => $"{action.ToString().ToLowerInvariant()}-{member.Name.ToLowerInvariant()}";

            var document = documentProvider.GetDocument(null, options);

            document.AssertAndGetOperation("send-publishtenantcreated", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
        }

        [Fact]
        public void GenerateDocument_UsesCustomChannelIdGenerator()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ChannelIdGeneratorPublisher));
            options.Inference.ChannelIdGenerator = address => $"chan.{address.Replace('.', '-')}";

            var document = documentProvider.GetDocument(null, options);

            var channel = document.AssertAndGetChannel("chan.tenant-created", "tenant.created");
            document.AssertAndGetOperation("Publish", ByteBard.AsyncAPI.Models.AsyncApiAction.Send).ChannelId.ShouldBe(channel.Id);
        }

        [Fact]
        public void GenerateDocument_UsesLegacyOperationIdFallbackWhenInferenceDisabled()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(OperationIdGeneratorPublisher));
            options.Inference.InferOperationIdFromMemberName = false;

            var document = documentProvider.GetDocument(null, options);

            document.AssertAndGetOperation("OperationIdGeneratorPublisher.PublishTenantCreated.send", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
        }

        [Fact]
        public void GenerateDocument_ThrowsWhenChannelIdInferenceDisabled()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ChannelIdGeneratorPublisher));
            options.Inference.InferChannelIdFromAddress = false;

            var actual = () => documentProvider.GetDocument(null, options);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("enable channel id inference");
        }

        [Fact]
        public void GenerateDocument_DoesNotInferPayloadTypeWhenDisabled()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(PayloadInferencePublisher));
            options.Inference.InferPayloadTypeFromMethodSignature = false;

            var document = documentProvider.GetDocument(null, options);

            var operation = document.AssertAndGetOperation("Publish", ByteBard.AsyncAPI.Models.AsyncApiAction.Send);
            operation.MessageIds.ShouldBeEmpty();
            document.Components.Messages.ShouldBeEmpty();
        }

        [Fact]
        public void GenerateDocument_ThrowsWhenRouteInferenceDisabled()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(RouteDisabledPublisher));
            options.Inference.InferChannelAddressFromRoute = false;

            var actual = () => documentProvider.GetDocument(null, options);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Set [Channel(\"address\")] explicitly or enable route-based inference");
        }

        [Fact]
        public void GenerateDocument_DoesNotSetDefaultContentTypeWhenDisabled()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(OperationIdGeneratorPublisher));
            options.Inference.AutoSetDefaultContentType = false;

            var document = documentProvider.GetDocument(null, options);

            document.DefaultContentType.ShouldBeNull();
        }

        [AsyncApi]
        [Channel("classLevelExplicit", "tenant.class.created")]
        [SendOperation(OperationId = "ClassLevelExplicitOperation")]
        private class ClassLevelExplicitPublisher
        {
            public void Publish(ClassPayload payload)
            {
            }
        }

        [AsyncApi]
        private class FilterOverriddenInferencePublisher
        {
            [Channel("tenant.created")]
            [SendOperation(typeof(ClassPayload))]
            public void Publish()
            {
            }
        }

        [AsyncApi]
        private class OperationIdGeneratorPublisher
        {
            [Channel("tenant.created")]
            [SendOperation(typeof(ClassPayload))]
            public void PublishTenantCreated()
            {
            }
        }

        [AsyncApi]
        private class ChannelIdGeneratorPublisher
        {
            [Channel("tenant.created")]
            [SendOperation(typeof(ClassPayload))]
            public void Publish()
            {
            }
        }

        [AsyncApi]
        private class PayloadInferencePublisher
        {
            [Channel("tenant.payload")]
            [SendOperation]
            public void Publish(ClassPayload payload)
            {
            }
        }

        [AsyncApi]
        private class RouteDisabledPublisher
        {
            [Channel]
            [SendOperation(typeof(ClassPayload))]
            [HttpPost("api/tenants/{tenantId}/created")]
            public void Publish([FromRoute] Guid tenantId, [FromBody] ClassPayload payload)
            {
            }
        }

        private class ClassPayload
        {
            public Guid Id { get; set; }
        }

        private class InferenceOverrideDocumentFilter : IDocumentFilter
        {
            public void Apply(AsyncApiDocumentDescriptor document, DocumentFilterContext context)
            {
                var channel = document.Channels["tenantCreated"];
                document.Channels.Remove("tenantCreated");
                document.Channels["filteredChannel"] = channel with { Id = "filteredChannel" };

                var operation = document.Operations["Publish"];
                document.Operations.Remove("Publish");
                document.Operations["filteredPublish"] = operation with { ChannelId = "filteredChannel" };
            }
        }
    }
}
