using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using Microsoft.Extensions.Logging.Testing;
using Saunter.SharedKernel;
using Xunit;

namespace Saunter.Tests.SharedKernel
{
    public class DocumentSerializeClonerTests
    {
        private readonly AsyncApiDocumentSerializeCloner _cloner;

        public DocumentSerializeClonerTests()
        {
            _cloner = new AsyncApiDocumentSerializeCloner(new FakeLogger<AsyncApiDocumentSerializeCloner>());
        }

        [Fact]
        public void CloneProtype_ShouldCloneDocumentSuccessfully()
        {
            var prototype = new AsyncApiDocument
            {
                Id = "id document",
                Asyncapi = "3.0.0",
                Info = new AsyncApiInfo
                {
                    Version = "1.0.0",
                    Title = "title",
                    Description = "description",
                    License = new AsyncApiLicense
                    {
                        Url = new("http://localhost:9200"),
                        Name = "test",
                    },
                    Contact = new AsyncApiContact
                    {
                        Url = new("http://localhost:9201"),
                        Name = "contact",
                        Email = "gmail.ru",
                    },
                    TermsOfService = new("http://localhost:9202"),
                },
                DefaultContentType = "default/type",
                Servers =
                {
                    ["one"] = new AsyncApiServer
                    {
                        Host = "hellowa",
                        Description = "server desc",
                        Protocol = "kaffka",
                        ProtocolVersion = "0.0.1",
                        Tags = { new() { Name = "kaffka tag" } },
                        Variables =
                        {
                            ["var"] = new AsyncApiServerVariable
                            {
                                Default = "default",
                                Description = "default var",
                                Enum = { "q", "w", "e" },
                                Examples = { "example one" },
                            }
                        }
                    }
                },
                Components = new()
                {
                    Schemas =
                    {
                        ["payload"] = new AsyncApiJsonSchema
                        {
                            Title = "payload",
                            Type = SchemaType.String,
                            Description = "payload",
                        }
                    },
                    Messages =
                    {
                        ["message"] = new AsyncApiMessage
                        {
                            Name = "message",
                            Title = "message",
                            Payload = new AsyncApiMultiFormatSchema
                            {
                                Schema = new AsyncApiJsonSchemaReference("#/components/schemas/payload")
                            }
                        }
                    }
                },
                Channels =
                {
                    ["channel"] = new AsyncApiChannel
                    {
                        Address = "channel",
                        Description = "description channel",
                        Servers = { new AsyncApiServerReference("#/servers/one") },
                        Messages =
                        {
                            ["message"] = new AsyncApiMessageReference("#/components/messages/message")
                        }
                    }
                },
                Operations =
                {
                    ["operation"] = new AsyncApiOperation
                    {
                        Action = AsyncApiAction.Send,
                        Channel = new AsyncApiChannelReference("#/channels/channel"),
                        Summary = "my summary",
                        Messages =
                        {
                            new AsyncApiMessageReference("#/components/messages/message")
                        }
                    }
                }
            };

            var result = _cloner.CloneProtype(prototype);

            Assert.NotNull(result);
            Assert.Equal(prototype.Id, result.Id);
            Assert.Equal(prototype.Asyncapi, result.Asyncapi);
            Assert.Equal(prototype.Info.Title, result.Info.Title);
            Assert.Equal(prototype.Channels.Count, result.Channels.Count);
            Assert.Equal(prototype.Operations.Count, result.Operations.Count);
            Assert.Equal(prototype.Components.Messages.Count, result.Components.Messages.Count);
            Assert.Equal(prototype.Components.Schemas.Count, result.Components.Schemas.Count);
        }
    }
}
