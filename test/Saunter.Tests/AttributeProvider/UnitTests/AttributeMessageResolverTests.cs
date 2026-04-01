using System;
using System.Linq;
using System.Reflection;
using Saunter.AttributeProvider;
using Saunter.AttributeProvider.Attributes;
using Saunter.Options;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Saunter.SharedKernel.Interfaces;
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

            var resolution = resolver.ResolveForOperation(method, new SendOperationAttribute(), new AsyncApiInferenceOptions());

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

            var actual = () => resolver.ResolveForOperation(method, new SendOperationAttribute(), new AsyncApiInferenceOptions());

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("must generate an object schema");
        }

        [Fact]
        public void ResolveForOperation_TypeLevelDiscovery_IgnoresSpecialNameMembers()
        {
            var resolver = new AttributeMessageResolver(new AsyncApiSchemaGenerator());

            var resolution = resolver.ResolveForOperation(typeof(TypeLevelFixture).GetTypeInfo(), new SendOperationAttribute(), new AsyncApiInferenceOptions());

            resolution.MessageIds.ShouldBe(["orderCreated"]);
        }

        [Fact]
        public void ResolveForOperation_AcceptsAllOfHeaderSchemas()
        {
            var resolver = new AttributeMessageResolver(new AllOfSchemaGenerator());
            var method = typeof(MessageFixture).GetMethod(nameof(MessageFixture.Publish))!;

            var resolution = resolver.ResolveForOperation(method, new SendOperationAttribute(), new AsyncApiInferenceOptions());

            resolution.Messages.Single().HeadersSchemaId.ShouldBe("messageHeaders");
        }

        [Fact]
        public void ResolveForOperation_ThrowsWhenDuplicateMessageAttributesConflict()
        {
            var resolver = new AttributeMessageResolver(new AsyncApiSchemaGenerator());
            var method = typeof(MessageFixture).GetMethod(nameof(MessageFixture.PublishWithConflictingMessageDescriptors))!;

            var actual = () => resolver.ResolveForOperation(method, new SendOperationAttribute(), new AsyncApiInferenceOptions());

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Conflicting message descriptors");
        }

        [Fact]
        public void ResolveForOperation_TypeLevelInference_ThrowsWhenDuplicateSchemasConflict()
        {
            var resolver = new AttributeMessageResolver(new ConflictingSchemaGenerator());

            var actual = () => resolver.ResolveForOperation(
                typeof(ConflictingTypeLevelFixture).GetTypeInfo(),
                new SendOperationAttribute(),
                new AsyncApiInferenceOptions
                {
                    MessageNameGenerator = _ => "sharedPayload",
                    MessageTitleGenerator = _ => "Shared Payload",
                });

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Conflicting schema descriptors");
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

            [Message(typeof(OrderCreated), MessageId = "orderCreated", Summary = "first")]
            [Message(typeof(OrderCreated), MessageId = "orderCreated", Summary = "second")]
            public void PublishWithConflictingMessageDescriptors()
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

        private class TypeLevelFixture
        {
            public string Name { get; set; } = string.Empty;

            public void Publish(OrderCreated _)
            {
            }
        }

        private class ConflictingTypeLevelFixture
        {
            public void Publish(SharedPayloadOne _)
            {
            }

            public void PublishAgain(SharedPayloadTwo _)
            {
            }
        }

        private class SharedPayloadOne
        {
            public string Value { get; set; } = string.Empty;
        }

        private class SharedPayloadTwo
        {
            public int Value { get; set; }
        }

        private sealed class AllOfSchemaGenerator : IAsyncApiSchemaGenerator
        {
            public GeneratedSchemaDescriptors? Generate(Type type)
            {
                if (type == typeof(MessageHeaders))
                {
                    var root = new AsyncApiSchemaDescriptor
                    {
                        Id = "messageHeaders",
                    };
                    root.AllOf.Add(new AsyncApiSchemaDescriptor
                    {
                        Type = AsyncApiSchemaValueType.Object,
                    });

                    return new GeneratedSchemaDescriptors(root, [root]);
                }

                return new AsyncApiSchemaGenerator().Generate(type);
            }
        }

        private sealed class ConflictingSchemaGenerator : IAsyncApiSchemaGenerator
        {
            public GeneratedSchemaDescriptors? Generate(Type type)
            {
                if (type == typeof(SharedPayloadOne))
                {
                    var schema = new AsyncApiSchemaDescriptor
                    {
                        Id = "sharedPayload",
                        Type = AsyncApiSchemaValueType.String,
                    };

                    return new GeneratedSchemaDescriptors(schema, [schema]);
                }

                if (type == typeof(SharedPayloadTwo))
                {
                    var schema = new AsyncApiSchemaDescriptor
                    {
                        Id = "sharedPayload",
                        Type = AsyncApiSchemaValueType.Integer,
                    };

                    return new GeneratedSchemaDescriptors(schema, [schema]);
                }

                return new AsyncApiSchemaGenerator().Generate(type);
            }
        }
    }
}
