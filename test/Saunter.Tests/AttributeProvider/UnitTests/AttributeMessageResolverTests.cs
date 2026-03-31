using System;
using System.Linq;
using Saunter.AttributeProvider;
using Saunter.AttributeProvider.Attributes;
using Saunter.SharedKernel;
using Shouldly;
using Xunit;

namespace Saunter.Tests.AttributeProvider.UnitTests
{
    public class AttributeMessageResolverTests
    {
        [Fact]
        public void ResolveForOperation_ReturnsDescriptorWithSanitizedMessageIdAndMetadata()
        {
            var resolver = new AttributeMessageResolver(new AsyncApiSchemaGenerator());
            var method = typeof(MessageFixture).GetMethod(nameof(MessageFixture.Publish))!;

            var resolution = resolver.ResolveForOperation(method, new SendOperationAttribute());

            resolution.MessageIds.Single().ShouldBe("order_created.v1");
            var message = resolution.Messages.Single();
            message.ContentType.ShouldBe("application/cloudevents+json");
            message.CorrelationIdRef.ShouldBe("orderCorrelation");
            message.ExternalDocsUrl.ShouldBe("https://example.com/messages/order-created");
            message.HeadersSchemaId.ShouldBe("messageHeaders");
            resolution.Schemas.ShouldContain(schema => schema.Id == "orderCreated");
            resolution.Schemas.ShouldContain(schema => schema.Id == "messageHeaders");
        }

        [Fact]
        public void ResolveForOperation_ThrowsWhenHeadersSchemaIsNotObjectLike()
        {
            var resolver = new AttributeMessageResolver(new AsyncApiSchemaGenerator());
            var method = typeof(MessageFixture).GetMethod(nameof(MessageFixture.PublishWithPrimitiveHeaders))!;

            var actual = () => resolver.ResolveForOperation(method, new SendOperationAttribute());

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("must generate an object schema");
        }

        private class MessageFixture
        {
            [Message(typeof(OrderCreated), MessageId = "order/created.v1", HeadersType = typeof(MessageHeaders), CorrelationId = "orderCorrelation", ContentType = "application/cloudevents+json", ExternalDocs = "https://example.com/messages/order-created")]
            public void Publish()
            {
            }

            [Message(typeof(OrderCreated), HeadersType = typeof(string))]
            public void PublishWithPrimitiveHeaders()
            {
            }
        }

        private class OrderCreated
        {
            public string Id { get; set; } = string.Empty;
        }

        private class MessageHeaders
        {
            public string CorrelationId { get; set; } = string.Empty;
        }
    }
}
