using System;
using System.Text.Json.Nodes;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Shouldly;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class DocumentWriterTests
    {
        [Fact]
        public void WriteJson_UsesJsonSchemaNullabilityForAsyncApi3()
        {
            var writer = new AsyncApiDocumentWriter(new AsyncApiDocumentMapper(new global::Saunter.AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper())));
            var document = new AsyncApiDocumentDescriptor
            {
                Asyncapi = "3.1.0",
                Info = new AsyncApiInfoDescriptor
                {
                    Title = "test",
                    Version = "1.0.0"
                },
                Components = new AsyncApiComponentsDescriptor
                {
                    Schemas =
                    {
                        ["payload"] = new AsyncApiSchemaDescriptor
                        {
                            Id = "payload",
                            Type = AsyncApiSchemaValueType.Object,
                            Nullable = true
                        }
                    }
                }
            };

            var json = writer.WriteJson(document);

            json.ShouldNotContain("\"nullable\"");
            json.ShouldContain("\"oneOf\"");
            json.ShouldContain("\"type\": \"null\"");
        }

        [Fact]
        public void WriteJson_RewritesNullableComponentReferencesForAsyncApi3()
        {
            var writer = new AsyncApiDocumentWriter(new AsyncApiDocumentMapper(new global::Saunter.AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper())));
            var document = new AsyncApiDocumentDescriptor
            {
                Asyncapi = "3.0.0",
                Info = new AsyncApiInfoDescriptor
                {
                    Title = "test",
                    Version = "1.0.0"
                },
                Components = new AsyncApiComponentsDescriptor()
            };

            document.Components.Schemas["rabbitMqUser"] = new AsyncApiSchemaDescriptor
            {
                Id = "rabbitMqUser",
                Type = AsyncApiSchemaValueType.Object,
            };

            var payload = new AsyncApiSchemaDescriptor
            {
                Id = "payload",
                Type = AsyncApiSchemaValueType.Object,
            };
            payload.Properties["rabbitMqUser"] = new AsyncApiSchemaDescriptor
            {
                Nullable = true,
            };
            payload.Properties["rabbitMqUser"].AllOf.Add(new AsyncApiSchemaDescriptor
            {
                Reference = "#/components/schemas/rabbitMqUser"
            });
            document.Components.Schemas["payload"] = payload;

            var json = writer.WriteJson(document);
            var propertySchema = JsonNode.Parse(json)!["components"]!["schemas"]!["payload"]!["properties"]!["rabbitMqUser"]!;

            json.ShouldNotContain("\"nullable\"");
            propertySchema["oneOf"].ShouldNotBeNull();
            propertySchema["oneOf"]![0]!["allOf"]![0]!["$ref"]!.GetValue<string>().ShouldBe("#/components/schemas/rabbitMqUser");
            propertySchema["oneOf"]![1]!["type"]!.GetValue<string>().ShouldBe("null");
        }

        [Fact]
        public void WriteJson_ThrowsForUnsupportedAsyncApiVersion()
        {
            var writer = new AsyncApiDocumentWriter(new AsyncApiDocumentMapper(new global::Saunter.AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper())));
            var document = new AsyncApiDocumentDescriptor
            {
                Asyncapi = "4.0.0",
                Info = new AsyncApiInfoDescriptor
                {
                    Title = "test",
                    Version = "1.0.0"
                }
            };

            var actual = () => writer.WriteJson(document);

            Should.Throw<InvalidOperationException>(actual)
                .Message.ShouldContain("Unsupported AsyncAPI version");
        }
    }
}
