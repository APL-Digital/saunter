using System;
using System.Text.Json.Nodes;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Saunter.Bindings.AMQP;
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
            var originalReferencedComponent = document.Components.Schemas["rabbitMqUser"];

            var json = writer.WriteJson(document);
            var propertySchema = JsonNode.Parse(json)!["components"]!["schemas"]!["payload"]!["properties"]!["rabbitMqUser"]!;

            json.ShouldNotContain("\"nullable\"");
            propertySchema["oneOf"].ShouldNotBeNull();
            propertySchema["oneOf"]![0]!["allOf"]![0]!["$ref"]!.GetValue<string>().ShouldBe("#/components/schemas/rabbitMqUser");
            propertySchema["oneOf"]![1]!["type"]!.GetValue<string>().ShouldBe("null");
            document.Components.Schemas["rabbitMqUser"].ShouldBeSameAs(originalReferencedComponent);
            originalReferencedComponent.Id.ShouldBe("rabbitMqUser");
            originalReferencedComponent.Type.ShouldBe(AsyncApiSchemaValueType.Object);
            originalReferencedComponent.Nullable.ShouldBeFalse();
            originalReferencedComponent.OneOf.ShouldBeEmpty();
        }

        [Fact]
        public void WriteJson_IgnoresNullSchemaCollectionForAsyncApi3()
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
                Components = new AsyncApiComponentsDescriptor
                {
                    Schemas = null!
                }
            };

            var json = writer.WriteJson(document);

            json.ShouldContain("\"asyncapi\": \"3.0.0\"");
        }

        [Fact]
        public void WriteJson_MapsServerFieldsAndAmqpBindings()
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
                Servers =
                {
                    ["rabbitmq"] = new AsyncApiServerDescriptor
                    {
                        Host = "rabbitmq.example.com:5671",
                        PathName = "/production",
                        Protocol = "amqps",
                        ProtocolVersion = "0-9-1",
                        Title = "RabbitMQ",
                        Summary = "Primary broker",
                        Description = "TLS-enabled RabbitMQ broker.",
                        ExternalDocs = "https://example.com/docs/rabbitmq",
                        ExternalDocsDescription = "RabbitMQ deployment guide",
                        Bindings = new AsyncApiBindings<IServerBinding>
                        {
                            new AMQPServerBinding()
                        }
                    }
                }
            };

            var json = writer.WriteJson(document);
            var server = JsonNode.Parse(json)!["servers"]!["rabbitmq"]!;

            server["host"]!.GetValue<string>().ShouldBe("rabbitmq.example.com:5671");
            server["pathname"]!.GetValue<string>().ShouldBe("/production");
            server["protocol"]!.GetValue<string>().ShouldBe("amqps");
            server["protocolVersion"]!.GetValue<string>().ShouldBe("0-9-1");
            server["description"]!.GetValue<string>().ShouldBe("TLS-enabled RabbitMQ broker.");
            ((JsonObject)server["bindings"]!["amqp"]!).Count.ShouldBe(0);
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
