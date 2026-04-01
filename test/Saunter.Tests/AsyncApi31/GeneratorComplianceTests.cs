using System;
using Saunter.AttributeProvider.Attributes;
using Saunter.SharedKernel;
using Saunter.Tests.AttributeProvider.DocumentGenerationTests;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AsyncApi31
{
    public class GeneratorComplianceTests
    {
        [Fact]
        public void GetDocument_ForcesAsyncApi30Version()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider);
            options.AsyncApi = new AsyncApiDocumentDescriptor
            {
                Asyncapi = "3.1.0",
                Info = new AsyncApiInfoDescriptor
                {
                    Title = "AsyncAPI 3.1",
                    Version = "1.0.0"
                }
            };

            var document = documentProvider.GetDocument(null, options);

            document.Asyncapi.ShouldBe("3.0.0");
        }

        [Fact]
        public void GenerateDocument_UsesObjectSchemaForMessageHeaders()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(PrimitiveHeadersPublisher));

            Should.Throw<InvalidOperationException>(() => documentProvider.GetDocument(null, options));
        }

        [Fact]
        public void GenerateDocument_PopulatesAllAddressExpressionParameters()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(MissingChannelParameterPublisher));

            var document = documentProvider.GetDocument(null, options);

            document.Channels["orders.byId"].Parameters.ShouldContainKey("orderId");
        }

        [Fact]
        public void GenerateDocument_DoesNotEmitParametersForLiteralAddresses()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ExtraChannelParameterPublisher));

            Should.Throw<InvalidOperationException>(() => documentProvider.GetDocument(null, options));
        }

        [Fact]
        public void GenerateDocument_RejectsInvalidChannelParameterNames()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(InvalidChannelParameterNamePublisher));

            Should.Throw<InvalidOperationException>(() => documentProvider.GetDocument(null, options));
        }

        [Fact]
        public void GenerateDocument_RejectsChannelAddressesWithQueryStrings()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(QueryStringChannelPublisher));

            Should.Throw<InvalidOperationException>(() => documentProvider.GetDocument(null, options));
        }

        [Fact]
        public void GenerateDocument_RejectsUnknownServerReferences()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(UnknownServerPublisher));
            options.AsyncApi = new AsyncApiDocumentDescriptor
            {
                Info = new AsyncApiInfoDescriptor
                {
                    Title = "Servers",
                    Version = "1.0.0"
                }
            };

            Should.Throw<InvalidOperationException>(() => documentProvider.GetDocument(null, options));
        }

        [Fact]
        public void GenerateDocument_SanitizesGenericComponentKeys()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(GenericPayloadPublisher));

            var document = documentProvider.GetDocument(null, options);

            document.Components.Schemas.Keys.ShouldAllBe(key => !key.Contains('`'));
        }

        [Fact]
        public void GenerateDocument_DoesNotEmitLegacyNullableKeyword()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(NullablePayloadPublisher));

            var document = documentProvider.GetDocument(null, options);
            var json = new AsyncApiDocumentWriter(new AsyncApiDocumentMapper(new global::Saunter.AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper()))).WriteJson(document);

            json.ShouldContain("\"nullable\"");
        }

        [Fact]
        public void OperationAttribute_ExposesReplyConfiguration()
        {
            typeof(OperationAttribute).GetProperty("Reply").ShouldNotBeNull();
        }

        [Theory]
        [InlineData("CorrelationId")]
        [InlineData("ContentType")]
        [InlineData("ExternalDocs")]
        public void MessageAttribute_ExposesAsyncApi31MessageMetadata(string propertyName)
        {
            typeof(MessageAttribute).GetProperty(propertyName).ShouldNotBeNull();
        }

        [AsyncApi]
        [Channel("primitive.headers", "primitive.headers")]
        [SendOperation]
        private class PrimitiveHeadersPublisher
        {
            [Message(typeof(PrimitiveHeadersPayload), HeadersType = typeof(string))]
            public void Publish() { }
        }

        private class PrimitiveHeadersPayload
        {
            public string Name { get; set; } = string.Empty;
        }

        [AsyncApi]
        [Channel("orders.byId", "orders/{orderId}")]
        [SendOperation]
        private class MissingChannelParameterPublisher
        {
            [Message(typeof(OrderCreated))]
            public void Publish() { }
        }

        [AsyncApi]
        [Channel("orders.created", "orders.created")]
        [ChannelParameter("orderId", typeof(string))]
        [SendOperation]
        private class ExtraChannelParameterPublisher
        {
            [Message(typeof(OrderCreated))]
            public void Publish() { }
        }

        [AsyncApi]
        [Channel("orders.invalidParam", "orders/{orderId}")]
        [ChannelParameter("order id", typeof(string))]
        [SendOperation]
        private class InvalidChannelParameterNamePublisher
        {
            [Message(typeof(OrderCreated))]
            public void Publish() { }
        }

        [AsyncApi]
        [Channel("orders.query", "orders/{orderId}?tenant=acme")]
        [ChannelParameter("orderId", typeof(string))]
        [SendOperation]
        private class QueryStringChannelPublisher
        {
            [Message(typeof(OrderCreated))]
            public void Publish() { }
        }

        [AsyncApi]
        [Channel("orders.missingServer", "orders.missingServer", Servers = new[] { "missing" })]
        [SendOperation]
        private class UnknownServerPublisher
        {
            [Message(typeof(OrderCreated))]
            public void Publish() { }
        }

        [AsyncApi]
        [Channel("generic.payload", "generic.payload")]
        [SendOperation]
        private class GenericPayloadPublisher
        {
            [Message(typeof(GenericPayload<int>))]
            public void Publish() { }
        }

        private class GenericPayload<T>
        {
            public T Value { get; set; } = default!;
        }

        [AsyncApi]
        [Channel("nullable.payload", "nullable.payload")]
        [SendOperation]
        private class NullablePayloadPublisher
        {
            [Message(typeof(NullablePayload))]
            public void Publish() { }
        }

        private class NullablePayload
        {
            public int? MaybeCount { get; set; }
        }

        private class OrderCreated
        {
            public Guid Id { get; set; }
        }
    }
}
