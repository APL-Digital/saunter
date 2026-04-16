using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Saunter.AttributeProvider;
using Saunter.AttributeProvider.Attributes;
using Saunter.Options;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Saunter.SharedKernel.Interfaces;
using Shouldly;
using Xunit;

#nullable enable

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
                .Message.ShouldContain(typeof(string).FullName!);
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
        public void ResolveForOperation_UsesJsonPropertyNameAttribute_ForHeaderSchemaProperties()
        {
            var resolver = new AttributeMessageResolver(new AsyncApiSchemaGenerator());
            var method = typeof(MessageFixture).GetMethod(nameof(MessageFixture.PublishWithJsonNamedHeaders))!;

            var resolution = resolver.ResolveForOperation(method, new SendOperationAttribute(), new AsyncApiInferenceOptions());

            var schema = resolution.Schemas.Single(component => component.Id == "jsonNamedHeaders").Schema;
            schema.Properties.ShouldContainKey("X-Markus-Actor-External-Id");
            schema.Properties.ShouldContainKey("X-Markus-Actor-System");
            schema.Properties.ShouldNotContainKey("externalId");
            schema.Properties.ShouldNotContainKey("system");
        }

        [Fact]
        public void ResolveForOperation_ThrowsWhenDuplicateMessageAttributesConflict()
        {
            var resolver = new AttributeMessageResolver(new AsyncApiSchemaGenerator());
            var method = typeof(MessageFixture).GetMethod(nameof(MessageFixture.PublishWithConflictingMessageDescriptors))!;

            var actual = () => resolver.ResolveForOperation(method, new SendOperationAttribute(), new AsyncApiInferenceOptions());

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Existing definition:");
        }

        [Fact]
        public void ResolveForOperation_SkipsAttributeMessageWhenSchemaIdIsBlank()
        {
            var resolver = new AttributeMessageResolver(new BlankSchemaIdGenerator());
            var method = typeof(MessageFixture).GetMethod(nameof(MessageFixture.PublishWithoutExplicitMessageId))!;

            var resolution = resolver.ResolveForOperation(method, new SendOperationAttribute(), new AsyncApiInferenceOptions());

            resolution.MessageIds.ShouldBeEmpty();
            resolution.Messages.ShouldBeEmpty();
        }

        [Fact]
        public void ResolveForOperation_FallsBackToOperationPayloadTypeWhenAttributesYieldNoMessages()
        {
            var resolver = new AttributeMessageResolver(new BlankSchemaIdGenerator());
            var method = typeof(MessageFixture).GetMethod(nameof(MessageFixture.PublishWithoutExplicitMessageIdButInferredPayloadExists))!;

            var resolution = resolver.ResolveForOperation(
                method,
                new SendOperationAttribute(),
                new AsyncApiInferenceOptions
                {
                    InferPayloadTypeFromMethodSignature = true,
                });

            resolution.MessageIds.ShouldBe(["fallbackPayload"]);
            resolution.Messages.Single().PayloadSchemaId.ShouldBe("fallbackPayload");
        }

        [Fact]
        public void ResolveForOperation_TypeLevelFallsBackToOperationPayloadTypeWhenAttributesYieldNoMessages()
        {
            var resolver = new AttributeMessageResolver(new BlankSchemaIdGenerator());

            var resolution = resolver.ResolveForOperation(
                typeof(TypeLevelBlankAttributeFixture).GetTypeInfo(),
                new SendOperationAttribute(),
                new AsyncApiInferenceOptions
                {
                    InferPayloadTypeFromMethodSignature = true,
                });

            resolution.MessageIds.ShouldBe(["fallbackPayload"]);
            resolution.Messages.Single().PayloadSchemaId.ShouldBe("fallbackPayload");
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
                .Message.ShouldContain("Existing definition:");
        }

        [Fact]
        public void ResolveForOperation_RegistersWrapperSchemaForNullableRootPayloads()
        {
            var resolver = new AttributeMessageResolver(new AsyncApiSchemaGenerator());
            var method = typeof(MessageFixture).GetMethod(nameof(MessageFixture.PublishNullablePrimitivePayload))!;

            var resolution = resolver.ResolveForOperation(method, new SendOperationAttribute(), new AsyncApiInferenceOptions());

            resolution.Messages.Single().PayloadSchemaId.ShouldBe("int32Nullable");
            resolution.Schemas.ShouldContain(schema => schema.Id == "int32");
            resolution.Schemas.ShouldContain(schema => schema.Id == "int32Nullable" && schema.Schema.Nullable);
        }

        [Fact]
        public void ResolveForOperation_TypeLevelInference_UsesDistinctNestedSchemaIdsAcrossMessages()
        {
            var resolver = new AttributeMessageResolver(new AsyncApiSchemaGenerator());

            var resolution = resolver.ResolveForOperation(
                typeof(NestedSchemaCollisionFixture).GetTypeInfo(),
                new SendOperationAttribute(),
                new AsyncApiInferenceOptions());

            resolution.MessageIds.ShouldBe(["addItem", "addModifier"], ignoreOrder: true);

            var addItemSchema = resolution.Schemas.Single(schema => schema.Id == "addItem").Schema;
            var addModifierSchema = resolution.Schemas.Single(schema => schema.Id == "addModifier").Schema;

            var addItemModifierId = GetReferencedSchemaId(addItemSchema.Properties["modifier"]);
            var addModifierModifierId = GetReferencedSchemaId(addModifierSchema.Properties["modifier"]);
            addItemModifierId.ShouldNotBeNull();
            addModifierModifierId.ShouldNotBeNull();
            addItemModifierId.ShouldNotBe(addModifierModifierId);
            resolution.Schemas.ShouldContain(schema => schema.Id == addItemModifierId);
            resolution.Schemas.ShouldContain(schema => schema.Id == addModifierModifierId);

            var addItemModifierSchema = ResolveComponentSchema(resolution.Schemas, addItemSchema.Properties["modifier"]);
            var addModifierSchemaComponent = ResolveComponentSchema(resolution.Schemas, addModifierSchema.Properties["modifier"]);
            var addItemMetadataId = GetReferencedSchemaId(addItemModifierSchema.Properties["metadata"]);
            var addModifierMetadataId = GetReferencedSchemaId(addModifierSchemaComponent.Properties["metadata"]);
            addItemMetadataId.ShouldNotBeNull();
            addModifierMetadataId.ShouldNotBeNull();
            addItemMetadataId.ShouldNotBe(addModifierMetadataId);
            resolution.Schemas.ShouldContain(schema => schema.Id == addItemMetadataId);
            resolution.Schemas.ShouldContain(schema => schema.Id == addModifierMetadataId);

            ResolveComponentSchema(resolution.Schemas, addItemModifierSchema.Properties["metadata"]).Properties["note"].Format.ShouldBe("string");
            ResolveComponentSchema(resolution.Schemas, addModifierSchemaComponent.Properties["metadata"]).Properties["note"].Format.ShouldBe("string");
        }

        [Fact]
        public void ResolveForOperation_TypeLevelInference_DeduplicatesSharedNestedClrTypeAcrossMessages()
        {
            var resolver = new AttributeMessageResolver(new AsyncApiSchemaGenerator());

            var resolution = resolver.ResolveForOperation(
                typeof(SharedNestedSchemaFixture).GetTypeInfo(),
                new SendOperationAttribute(),
                new AsyncApiInferenceOptions());

            resolution.MessageIds.ShouldBe(["sharedMetadataOne", "sharedMetadataTwo"], ignoreOrder: true);

            var firstPayload = resolution.Schemas.Single(schema => schema.Id == "sharedMetadataOne").Schema;
            var secondPayload = resolution.Schemas.Single(schema => schema.Id == "sharedMetadataTwo").Schema;
            var sharedMetadataId = GetReferencedSchemaId(firstPayload.Properties["metadata"]);

            sharedMetadataId.ShouldNotBeNull();
            GetReferencedSchemaId(secondPayload.Properties["metadata"]).ShouldBe(sharedMetadataId);
            resolution.Schemas.Count(schema => schema.Id == sharedMetadataId).ShouldBe(1);
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

            [Message(typeof(OrderCreated), HeadersType = typeof(JsonNamedHeaders))]
            public void PublishWithJsonNamedHeaders()
            {
            }

            [Message(typeof(OrderCreated), MessageId = "orderCreated", Summary = "first")]
            [Message(typeof(OrderCreated), MessageId = "orderCreated", Summary = "second")]
            public void PublishWithConflictingMessageDescriptors()
            {
            }

            [Message(typeof(OrderCreated))]
            public void PublishWithoutExplicitMessageId()
            {
            }

            [Message(typeof(OrderCreated))]
            public void PublishWithoutExplicitMessageIdButInferredPayloadExists(FallbackPayload _)
            {
            }

            [Message(typeof(int?))]
            public void PublishNullablePrimitivePayload()
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

        private class JsonNamedHeaders
        {
            [JsonPropertyName("X-Markus-Actor-External-Id")]
            public string? ExternalId { get; set; }

            [JsonPropertyName("X-Markus-Actor-System")]
            public bool? System { get; set; }
        }

        private class TypeLevelFixture
        {
            public string Name { get; set; } = string.Empty;

            public void Publish(OrderCreated _)
            {
            }
        }

        private class TypeLevelBlankAttributeFixture
        {
            [Message(typeof(OrderCreated))]
            public void Publish(FallbackPayload _)
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

        private class NestedSchemaCollisionFixture
        {
            public void Publish(global::Saunter.Tests.AttributeProvider.UnitTests.NestedSchemaCollisionSamples.AddItem.AddItem _)
            {
            }

            public void PublishAgain(global::Saunter.Tests.AttributeProvider.UnitTests.NestedSchemaCollisionSamples.AddModifier.AddModifier _)
            {
            }
        }

        private class SharedNestedSchemaFixture
        {
            public void Publish(global::Saunter.Tests.AttributeProvider.UnitTests.SharedNestedSchemaSamples.SharedMetadataOne _)
            {
            }

            public void PublishAgain(global::Saunter.Tests.AttributeProvider.UnitTests.SharedNestedSchemaSamples.SharedMetadataTwo _)
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

        private class FallbackPayload
        {
            public string Value { get; set; } = string.Empty;
        }

        private sealed class AllOfSchemaGenerator : IAsyncApiSchemaGenerator
        {
            public GeneratedSchemaDescriptors? Generate(Type? type)
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
            public GeneratedSchemaDescriptors? Generate(Type? type)
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

        private sealed class BlankSchemaIdGenerator : IAsyncApiSchemaGenerator
        {
            public GeneratedSchemaDescriptors? Generate(Type? type)
            {
                if (type == typeof(OrderCreated))
                {
                    var schema = new AsyncApiSchemaDescriptor
                    {
                        Id = string.Empty,
                        Type = AsyncApiSchemaValueType.Object,
                    };

                    return new GeneratedSchemaDescriptors(schema, [schema]);
                }

                return new AsyncApiSchemaGenerator().Generate(type);
            }
        }

        private static string? GetReferencedSchemaId(AsyncApiSchemaDescriptor schema)
        {
            if (!string.IsNullOrWhiteSpace(schema.Id))
            {
                return schema.Id;
            }

            return schema.AllOf
                .Select(item => item.Reference)
                .FirstOrDefault(reference => reference?.StartsWith("#/components/schemas/", StringComparison.Ordinal) == true)?
                ["#/components/schemas/".Length..];
        }

        private static AsyncApiSchemaDescriptor ResolveComponentSchema(
            System.Collections.Generic.IReadOnlyList<Saunter.AttributeProvider.Descriptors.AsyncApiSchemaComponentDescriptor> schemas,
            AsyncApiSchemaDescriptor schema)
        {
            var schemaId = GetReferencedSchemaId(schema);
            schemaId.ShouldNotBeNull();
            return schemas.Single(component => component.Id == schemaId).Schema;
        }
    }
}

namespace Saunter.Tests.AttributeProvider.UnitTests.NestedSchemaCollisionSamples.AddItem
{
    public class AddItem
    {
        public Modifier Modifier { get; set; } = null!;
    }

    public class Modifier
    {
        public Metadata Metadata { get; set; } = null!;
    }

    public class Metadata
    {
        public string Note { get; set; } = string.Empty;
    }
}

namespace Saunter.Tests.AttributeProvider.UnitTests.NestedSchemaCollisionSamples.AddModifier
{
    public class AddModifier
    {
        public Modifier Modifier { get; set; } = null!;
    }

    public class Modifier
    {
        public Metadata Metadata { get; set; } = null!;
    }

    public class Metadata
    {
        public string Note { get; set; } = string.Empty;
    }
}

namespace Saunter.Tests.AttributeProvider.UnitTests.SharedNestedSchemaSamples
{
    public class SharedMetadataOne
    {
        public SharedMetadata Metadata { get; set; } = null!;
    }

    public class SharedMetadataTwo
    {
        public SharedMetadata Metadata { get; set; } = null!;
    }

    public class SharedMetadata
    {
        public string Source { get; set; } = string.Empty;
    }
}
