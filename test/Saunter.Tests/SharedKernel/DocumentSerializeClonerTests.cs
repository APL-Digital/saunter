using System.Collections.Generic;
using ByteBard.AsyncAPI.Bindings.AMQP;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Microsoft.Extensions.Logging.Testing;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Bindings.AMQP;
using Saunter.SharedKernel;
using Saunter.SharedKernel.Descriptors;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class DocumentSerializeClonerTests
    {
        private readonly AsyncApiDocumentSerializeCloner _cloner;

        public DocumentSerializeClonerTests()
        {
            _cloner = new AsyncApiDocumentSerializeCloner(
                new FakeLogger<AsyncApiDocumentSerializeCloner>(),
                new AsyncApiDocumentWriter(new AsyncApiDocumentMapper(new global::Saunter.AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper()))));
        }

        [Fact]
        public void ClonePrototype_ShouldCloneDocumentSuccessfully()
        {
            var prototype = new AsyncApiDocumentDescriptor
            {
                Id = "id document",
                Asyncapi = "3.0.0",
                Info = new AsyncApiInfoDescriptor
                {
                    Version = "1.0.0",
                    Title = "title",
                    Description = "description",
                    License = new AsyncApiLicenseDescriptor
                    {
                        Url = new("http://localhost:9200"),
                        Name = "test",
                    },
                    Contact = new AsyncApiContactDescriptor
                    {
                        Url = new("http://localhost:9201"),
                        Name = "contact",
                        Email = "test@example.com",
                    },
                    TermsOfService = new("http://localhost:9202"),
                },
                DefaultContentType = "default/type",
                Servers =
                {
                    ["one"] = new AsyncApiServerDescriptor
                    {
                        Host = "hellowa",
                        PathName = "/events",
                        Description = "server desc",
                        Protocol = "kafka",
                        ProtocolVersion = "0.0.1",
                        Title = "server title",
                        Summary = "server summary",
                        ExternalDocs = "https://example.com/servers/one",
                        ExternalDocsDescription = "server docs",
                        BindingsRef = "rabbitmq",
                        Tags = { new() { Name = "kafka tag" } },
                        Variables =
                        {
                            ["var"] = new AsyncApiServerVariableDescriptor
                            {
                                Default = "default",
                                Description = "default var",
                                Enum = { "q", "w", "e" },
                                Examples = { "example one" },
                            }
                        }
                    }
                },
                Components = new AsyncApiComponentsDescriptor
                {
                    Schemas =
                    {
                        ["payload"] = new AsyncApiSchemaDescriptor
                        {
                            Id = "payload",
                            Type = AsyncApiSchemaValueType.String,
                        }
                    },
                    Messages =
                    {
                        ["message"] = new AsyncApiMessageDescriptor("message", "message", "message", null, null, "payload", null, null, null, null, null, null, [])
                        {
                            Bindings = new AsyncApiBindings<IMessageBinding>
                            {
                                new AMQPMessageBinding { MessageType = "message.created" }
                            }
                        }
                    },
                    ServerBindings =
                    {
                        ["rabbitmq"] = new AsyncApiBindings<IServerBinding>
                        {
                            new AMQPServerBinding()
                        }
                    }
                },
                Channels =
                {
                    ["channel"] = new AsyncApiChannelDescriptor("channel", "channel", null, null, "description channel", null, ["one"], ["message"], [])
                    {
                        Bindings = new AsyncApiBindings<IChannelBinding>
                        {
                            new AMQPChannelBinding { Is = ChannelType.Queue }
                        }
                    }
                },
                Operations =
                {
                    ["operation"] = new AsyncApiOperationDescriptor(ByteBard.AsyncAPI.Models.AsyncApiAction.Send, "channel", null, "my summary", null, null, ["message"], [], null)
                    {
                        Bindings = new AsyncApiBindings<IOperationBinding>
                        {
                            new AMQPOperationBinding { Ack = true }
                        }
                    }
                }
            };

            var result = _cloner.ClonePrototype(prototype);

            Assert.NotNull(result);
            Assert.Equal(prototype.Id, result.Id);
            Assert.Equal(prototype.Asyncapi, result.Asyncapi);
            Assert.Equal(prototype.Info.Title, result.Info.Title);
            Assert.Equal(prototype.Channels.Count, result.Channels.Count);
            Assert.Equal(prototype.Operations.Count, result.Operations.Count);
            Assert.Equal(prototype.Components.Messages.Count, result.Components.Messages.Count);
            Assert.Equal(prototype.Components.Schemas.Count, result.Components.Schemas.Count);
            Assert.Equal("/events", result.Servers["one"].PathName);
            Assert.Equal("server title", result.Servers["one"].Title);
            Assert.Equal("server summary", result.Servers["one"].Summary);
            Assert.Equal("https://example.com/servers/one", result.Servers["one"].ExternalDocs);
            Assert.Equal("server docs", result.Servers["one"].ExternalDocsDescription);
            Assert.Equal("rabbitmq", result.Servers["one"].BindingsRef);
            Assert.True(result.Components.ServerBindings.ContainsKey("rabbitmq"));
            Assert.True(result.Components.Messages["message"].Bindings.ContainsKey("amqp"));
            Assert.True(result.Channels["channel"].Bindings.ContainsKey("amqp"));
            Assert.True(result.Operations["operation"].Bindings.ContainsKey("amqp"));
        }

        [Fact]
        public void ClonePrototype_HandlesNullSchemaCollection()
        {
            var prototype = new AsyncApiDocumentDescriptor
            {
                Components = new AsyncApiComponentsDescriptor
                {
                    Schemas = null!
                }
            };

            var result = _cloner.ClonePrototype(prototype);

            Assert.NotNull(result.Components);
            Assert.NotNull(result.Components.Schemas);
            Assert.Empty(result.Components.Schemas);
        }

        [Fact]
        public void ClonePrototype_PreservesAdditionalPropertiesOnMapSchemas()
        {
            var prototype = new AsyncApiDocumentDescriptor
            {
                Components = new AsyncApiComponentsDescriptor
                {
                    Schemas =
                    {
                        ["metadata"] = new AsyncApiSchemaDescriptor
                        {
                            Id = "metadata",
                            Type = AsyncApiSchemaValueType.Object,
                            AdditionalProperties = new AsyncApiSchemaDescriptor
                            {
                                Type = AsyncApiSchemaValueType.String,
                            },
                        }
                    }
                }
            };

            var result = _cloner.ClonePrototype(prototype);

            var cloned = result.Components.Schemas["metadata"];
            Assert.NotNull(cloned.AdditionalProperties);
            Assert.Equal(AsyncApiSchemaValueType.String, cloned.AdditionalProperties!.Type);
        }
    }
}
