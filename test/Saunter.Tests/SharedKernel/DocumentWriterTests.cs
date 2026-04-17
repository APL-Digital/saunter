using System;
using System.Text.Json.Nodes;
using ByteBard.AsyncAPI.Bindings.AMQP;
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
        public void WriteJson_SerializesInlineAmqpChannelOperationAndMessageBindings()
        {
            var writer = new AsyncApiDocumentWriter(new AsyncApiDocumentMapper(new global::Saunter.AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper())));
            var document = new AsyncApiDocumentDescriptor
            {
                Asyncapi = "3.0.0",
                Info = new AsyncApiInfoDescriptor
                {
                    Title = "Minimal AMQP Feature Coverage",
                    Version = "1.0.0"
                },
                Servers =
                {
                    ["rabbitmq"] = new AsyncApiServerDescriptor
                    {
                        Host = "localhost:5672",
                        Protocol = "amqp"
                    }
                },
                Components = new AsyncApiComponentsDescriptor
                {
                    Schemas =
                    {
                        ["signupPayload"] = CreatePayloadSchema("signupPayload"),
                        ["queuedSignupPayload"] = CreatePayloadSchema("queuedSignupPayload"),
                    },
                    Messages =
                    {
                        ["signupMessage"] = new global::Saunter.AttributeProvider.Descriptors.AsyncApiMessageDescriptor("signupMessage", "signupMessage", "Signup event", null, null, "signupPayload", null, null, null, null, null, null, [])
                        {
                            Bindings = new AsyncApiBindings<IMessageBinding>
                            {
                                new AMQPMessageBinding
                                {
                                    ContentEncoding = "gzip",
                                    MessageType = "user.signup",
                                    BindingVersion = "0.3.0",
                                }
                            }
                        },
                        ["queuedSignupMessage"] = new global::Saunter.AttributeProvider.Descriptors.AsyncApiMessageDescriptor("queuedSignupMessage", "queuedSignupMessage", "Queued signup event", null, null, "queuedSignupPayload", null, null, null, null, null, null, [])
                        {
                            Bindings = new AsyncApiBindings<IMessageBinding>
                            {
                                new AMQPMessageBinding
                                {
                                    ContentEncoding = "gzip",
                                    MessageType = "user.signup.queued",
                                    BindingVersion = "0.3.0",
                                }
                            }
                        },
                    }
                },
                Channels =
                {
                    ["routedChannel"] = new global::Saunter.AttributeProvider.Descriptors.AsyncApiChannelDescriptor("routedChannel", "user.signup", null, null, null, null, ["rabbitmq"], ["signupMessage"], [])
                    {
                        Bindings = new AsyncApiBindings<IChannelBinding>
                        {
                            new AMQPChannelBinding
                            {
                                Is = ChannelType.RoutingKey,
                                Exchange = new Exchange
                                {
                                    Name = "user.events",
                                    Type = ExchangeType.Topic,
                                    Durable = true,
                                    AutoDelete = false,
                                    Vhost = "/",
                                },
                                BindingVersion = "0.3.0",
                            }
                        }
                    },
                    ["queueChannel"] = new global::Saunter.AttributeProvider.Descriptors.AsyncApiChannelDescriptor("queueChannel", "signup.queue", null, null, null, null, ["rabbitmq"], ["queuedSignupMessage"], [])
                    {
                        Bindings = new AsyncApiBindings<IChannelBinding>
                        {
                            new AMQPChannelBinding
                            {
                                Is = ChannelType.Queue,
                                Queue = new Queue
                                {
                                    Name = "signup.queue",
                                    Durable = true,
                                    Exclusive = false,
                                    AutoDelete = false,
                                    Vhost = "/",
                                },
                                BindingVersion = "0.3.0",
                            }
                        }
                    }
                },
                Operations =
                {
                    ["receiveSignup"] = new global::Saunter.AttributeProvider.Descriptors.AsyncApiOperationDescriptor(AsyncApiAction.Receive, "routedChannel", null, null, null, null, ["signupMessage"], [], null)
                    {
                        Bindings = new AsyncApiBindings<IOperationBinding>
                        {
                            new AMQPOperationBinding
                            {
                                Expiration = 60000,
                                UserId = "guest",
                                Cc = { "user.audit" },
                                Priority = 5,
                                DeliveryMode = DeliveryMode.Persistent,
                                Mandatory = true,
                                Bcc = { "internal.audit" },
                                Timestamp = true,
                                Ack = true,
                                BindingVersion = "0.3.0",
                            }
                        }
                    },
                    ["sendQueuedSignup"] = new global::Saunter.AttributeProvider.Descriptors.AsyncApiOperationDescriptor(AsyncApiAction.Send, "queueChannel", null, null, null, null, ["queuedSignupMessage"], [], null)
                }
            };

            var root = JsonNode.Parse(writer.WriteJson(document))!;

            root["channels"]!["routedChannel"]!["bindings"]!["amqp"]!["is"]!.GetValue<string>().ShouldBe("routingKey");
            root["channels"]!["queueChannel"]!["bindings"]!["amqp"]!["queue"]!["name"]!.GetValue<string>().ShouldBe("signup.queue");
            root["operations"]!["receiveSignup"]!["bindings"]!["amqp"]!["deliveryMode"]!.GetValue<int>().ShouldBe(2);
            root["components"]!["messages"]!["signupMessage"]!["bindings"]!["amqp"]!["messageType"]!.GetValue<string>().ShouldBe("user.signup");
            root["components"]!["messages"]!["queuedSignupMessage"]!["bindings"]!["amqp"]!["messageType"]!.GetValue<string>().ShouldBe("user.signup.queued");
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

        private static AsyncApiSchemaDescriptor CreatePayloadSchema(string id)
        {
            var schema = new AsyncApiSchemaDescriptor
            {
                Id = id,
                Type = AsyncApiSchemaValueType.Object,
            };
            schema.Properties["userId"] = new AsyncApiSchemaDescriptor
            {
                Type = AsyncApiSchemaValueType.String,
            };

            return schema;
        }
    }
}
