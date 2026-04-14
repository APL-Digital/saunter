using System;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options;
using Saunter.SharedKernel.Descriptors;
using Saunter.SharedKernel.Interfaces;
using Saunter.Tests.AttributeProvider.DocumentGenerationTests;
using Saunter.Tests.MarkerTypeTests;
using Shouldly;
using Xunit;

#nullable enable

namespace Saunter.Tests.AttributeProvider.DocumentProviderTests
{
    public class AsyncApiTypesTests
    {
        [Fact]
        public void GetDocument_GeneratesDocumentWithMultipleMessagesPerChannel()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(o =>
            {
                o.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = GetType().FullName,
                        Version = "1.0.0"
                    },
                };
                o.AssemblyMarkerTypes = new[] { typeof(AnotherSamplePublisher), typeof(SampleConsumer) };
            });

            using var serviceprovider = services.BuildServiceProvider();

            var documentProvider = serviceprovider.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = serviceprovider.GetRequiredService<IOptions<AsyncApiOptions>>().Value;
            var document = documentProvider.GetDocument(null, options);

            document.ShouldNotBeNull();
            document.Channels.ShouldContainKey("asw.sample_service.anothersample");
            document.Operations.ShouldContainKey("AnotherSampleMessagePublisher");
            document.Operations.ShouldContainKey("SampleMessageConsumer");
        }

        [Fact]
        public void GetDocument_ThrowsWithDetailedOperationConflict()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(o =>
            {
                o.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = GetType().FullName,
                        Version = "1.0.0"
                    },
                };
                o.AssemblyMarkerTypes = new[] { typeof(ConflictingPublishOne), typeof(ConflictingPublishTwo) };
            });

            using var serviceprovider = services.BuildServiceProvider();

            var documentProvider = serviceprovider.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = serviceprovider.GetRequiredService<IOptions<AsyncApiOptions>>().Value;

            var actual = () => documentProvider.GetDocument(null, options);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Existing definition:");
        }

        [Fact]
        public void GetDocument_ThrowsWithDetailedOperationConflictAgainstPreconfiguredOperation()
        {
            var services = new ServiceCollection();

            services.AddFakeLogging();
            services.AddAsyncApiSchemaGeneration(o =>
            {
                o.AsyncApi = new AsyncApiDocumentDescriptor
                {
                    Asyncapi = "3.0.0",
                    Info = new AsyncApiInfoDescriptor
                    {
                        Title = GetType().FullName,
                        Version = "1.0.0"
                    },
                    Operations =
                    {
                        ["Publish"] = new AsyncApiOperationDescriptor(ByteBard.AsyncAPI.Models.AsyncApiAction.Send, "existingChannel", null, null, null, null, [], [], null)
                    }
                };
                o.AssemblyMarkerTypes = new[] { typeof(PreconfiguredConflictPublisher) };
            });

            using var serviceprovider = services.BuildServiceProvider();

            var documentProvider = serviceprovider.GetRequiredService<IAsyncApiDocumentProvider>();
            var options = serviceprovider.GetRequiredService<IOptions<AsyncApiOptions>>().Value;

            var actual = () => documentProvider.GetDocument(null, options);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("preconfigured document operation");
        }

        [Fact]
        public void GetDocument_UsesFullTypeNameForClassLevelOperationConflictSources()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(ClassLevelConflictOne), typeof(ClassLevelConflictTwo));

            var actual = () => documentProvider.GetDocument(null, options);

            var error = Should.Throw<InvalidOperationException>(actual);
            error.Message.ShouldContain(typeof(ClassLevelConflictOne).FullName!);
            error.Message.ShouldContain(typeof(ClassLevelConflictTwo).FullName!);
        }

        [Fact]
        public void GetDocument_GeneratesDistinctNestedSchemaIdsForOverlappingPayloadGraphs()
        {
            ArrangeAttributesTests.Arrange(out var options, out var documentProvider, typeof(NestedSchemaCollisionPublisher));

            using var serviceprovider = new ServiceCollection()
                .AddFakeLogging()
                .AddAsyncApiSchemaGeneration()
                .BuildServiceProvider();

            var writer = serviceprovider.GetRequiredService<IAsyncApiDocumentWriter>();
            var document = documentProvider.GetDocument(null, options);

            document.Components.Messages.ShouldContainKey("addItem");
            document.Components.Messages.ShouldContainKey("addModifier");
            document.Components.Schemas.ShouldContainKey("addItem");
            document.Components.Schemas.ShouldContainKey("addModifier");

            var addItemSchema = document.Components.Schemas["addItem"];
            var addModifierSchema = document.Components.Schemas["addModifier"];
            var addItemModifierId = GetReferencedSchemaId(addItemSchema.Properties["modifier"]);
            var addModifierModifierId = GetReferencedSchemaId(addModifierSchema.Properties["modifier"]);

            addItemModifierId.ShouldNotBeNull();
            addModifierModifierId.ShouldNotBeNull();
            addItemModifierId.ShouldNotBe(addModifierModifierId);
            document.Components.Schemas.ShouldContainKey(addItemModifierId);
            document.Components.Schemas.ShouldContainKey(addModifierModifierId);

            var addItemModifierSchema = ResolveComponentSchema(document.Components.Schemas, addItemSchema.Properties["modifier"]);
            var addModifierSchemaComponent = ResolveComponentSchema(document.Components.Schemas, addModifierSchema.Properties["modifier"]);
            var addItemMetadataId = GetReferencedSchemaId(addItemModifierSchema.Properties["metadata"]);
            var addModifierMetadataId = GetReferencedSchemaId(addModifierSchemaComponent.Properties["metadata"]);

            addItemMetadataId.ShouldNotBeNull();
            addModifierMetadataId.ShouldNotBeNull();
            addItemMetadataId.ShouldNotBe(addModifierMetadataId);
            document.Components.Schemas.ShouldContainKey(addItemMetadataId);
            document.Components.Schemas.ShouldContainKey(addModifierMetadataId);

            var json = writer.WriteJson(document);
            var root = JsonNode.Parse(json);
            root.ShouldNotBeNull();

            var schemas = root!["components"]!["schemas"]!.AsObject();
            schemas.ShouldContainKey("addItem");
            schemas.ShouldContainKey("addModifier");
            schemas.ShouldContainKey(addItemModifierId);
            schemas.ShouldContainKey(addModifierModifierId);
            schemas.ShouldContainKey(addItemMetadataId);
            schemas.ShouldContainKey(addModifierMetadataId);
            schemas[addItemMetadataId]!["properties"]!["note"].ShouldNotBeNull();
            schemas[addModifierMetadataId]!["properties"]!["note"].ShouldNotBeNull();
        }

        [AsyncApi]
        private class ConflictingPublishOne
        {
            [Channel("orders.created", "orders.created")]
            [SendOperation]
            [Message(typeof(ConflictPayload))]
            public void Publish()
            {
            }
        }

        [AsyncApi]
        private class ConflictingPublishTwo
        {
            [Channel("orders.updated", "orders.updated")]
            [SendOperation]
            [Message(typeof(ConflictPayload))]
            public void Publish()
            {
            }
        }

        private class ConflictPayload
        {
            public string Id { get; set; } = string.Empty;
        }

        [AsyncApi]
        [Channel("class.level.one", "class.level.one")]
        [SendOperation(typeof(ConflictPayload), OperationId = "ClassLevelConflict")]
        private class ClassLevelConflictOne
        {
        }

        [AsyncApi]
        [Channel("class.level.two", "class.level.two")]
        [SendOperation(typeof(ConflictPayload), OperationId = "ClassLevelConflict")]
        private class ClassLevelConflictTwo
        {
        }

        [AsyncApi]
        [Channel("shopping.cart.commands", "shopping.cart.commands")]
        [SendOperation]
        private class NestedSchemaCollisionPublisher
        {
            public void Publish(global::Saunter.Tests.AttributeProvider.DocumentProviderTests.NestedSchemaCollisionSamples.AddItem.AddItem _)
            {
            }

            public void PublishAgain(global::Saunter.Tests.AttributeProvider.DocumentProviderTests.NestedSchemaCollisionSamples.AddModifier.AddModifier _)
            {
            }
        }

        [AsyncApi]
        private class PreconfiguredConflictPublisher
        {
            [Channel("orders.preconfigured", "orders.preconfigured")]
            [SendOperation]
            [Message(typeof(ConflictPayload))]
            public void Publish()
            {
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
            System.Collections.Generic.IDictionary<string, AsyncApiSchemaDescriptor> schemas,
            AsyncApiSchemaDescriptor schema)
        {
            var schemaId = GetReferencedSchemaId(schema);
            schemaId.ShouldNotBeNull();
            return schemas[schemaId];
        }
    }
}

namespace Saunter.Tests.AttributeProvider.DocumentProviderTests.NestedSchemaCollisionSamples.AddItem
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

namespace Saunter.Tests.AttributeProvider.DocumentProviderTests.NestedSchemaCollisionSamples.AddModifier
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
