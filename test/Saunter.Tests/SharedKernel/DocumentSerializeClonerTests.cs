using System.Collections.Generic;
using Microsoft.Extensions.Logging.Testing;
using Saunter.AttributeProvider.Descriptors;
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
                new AsyncApiDocumentWriter());
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
                        Email = "gmail.ru",
                    },
                    TermsOfService = new("http://localhost:9202"),
                },
                DefaultContentType = "default/type",
                Servers =
                {
                    ["one"] = new AsyncApiServerDescriptor
                    {
                        Host = "hellowa",
                        Description = "server desc",
                        Protocol = "kafka",
                        ProtocolVersion = "0.0.1",
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
                    }
                },
                Channels =
                {
                    ["channel"] = new AsyncApiChannelDescriptor("channel", "channel", null, null, "description channel", null, ["one"], ["message"], [])
                },
                Operations =
                {
                    ["operation"] = new AsyncApiOperationDescriptor(ByteBard.AsyncAPI.Models.AsyncApiAction.Send, "channel", null, "my summary", null, null, ["message"], [], null)
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
        }
    }
}
