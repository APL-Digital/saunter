using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Saunter.AttributeProvider;
using Saunter.AttributeProvider.Descriptors;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiDocumentMapper : IAsyncApiDocumentMapper
    {
        private readonly IAsyncApiDescriptorMapper _descriptorMapper;

        public AsyncApiDocumentMapper(IAsyncApiDescriptorMapper descriptorMapper)
        {
            _descriptorMapper = descriptorMapper;
        }

        public AsyncApiDocument Map(AsyncApiDocumentDescriptor document)
        {
            var mapped = new AsyncApiDocument
            {
                Id = document.Id,
                Asyncapi = document.Asyncapi,
                DefaultContentType = document.DefaultContentType,
                Info = MapInfo(document.Info),
                Components = new AsyncApiComponents
                {
                    OperationBindings = new Dictionary<string, AsyncApiBindings<IOperationBinding>>(document.Components.OperationBindings),
                    MessageBindings = new Dictionary<string, AsyncApiBindings<IMessageBinding>>(document.Components.MessageBindings),
                    ChannelBindings = new Dictionary<string, AsyncApiBindings<IChannelBinding>>(document.Components.ChannelBindings),
                    OperationTraits = new Dictionary<string, AsyncApiOperationTrait>(document.Components.OperationTraits),
                    CorrelationIds = new Dictionary<string, AsyncApiCorrelationId>(document.Components.CorrelationIds),
                    SecuritySchemes = new Dictionary<string, AsyncApiSecurityScheme>(document.Components.SecuritySchemes),
                },
                Servers = document.Servers.ToDictionary(pair => pair.Key, pair => MapServer(pair.Value)),
                Channels = new Dictionary<string, AsyncApiChannel>(),
                Operations = new Dictionary<string, AsyncApiOperation>(),
            };

            var messageResolution = new AsyncApiMessageResolutionDescriptor(
                document.Components.Messages.Keys.ToArray(),
                document.Components.Messages.Values.ToArray(),
                document.Components.Schemas.Select(pair => new AsyncApiSchemaComponentDescriptor(pair.Key, pair.Value)).ToArray());
            _descriptorMapper.RegisterMessageResolution(mapped.Components, messageResolution);

            foreach (var pair in document.Channels)
            {
                mapped.Channels[pair.Key] = _descriptorMapper.MapChannel(mapped.Components, pair.Value);
            }

            foreach (var pair in document.Operations)
            {
                mapped.Operations[pair.Key] = _descriptorMapper.MapOperation(pair.Value);
            }

            return mapped;
        }

        private static AsyncApiInfo MapInfo(AsyncApiInfoDescriptor info)
        {
            return new AsyncApiInfo
            {
                Version = info.Version,
                Title = info.Title,
                Description = info.Description,
                Contact = info.Contact is null ? null : new AsyncApiContact
                {
                    Email = info.Contact.Email,
                    Name = info.Contact.Name,
                    Url = info.Contact.Url,
                },
                License = info.License is null ? null : new AsyncApiLicense
                {
                    Name = info.License.Name,
                    Url = info.License.Url,
                },
                TermsOfService = info.TermsOfService,
            };
        }

        private static AsyncApiServer MapServer(AsyncApiServerDescriptor server)
        {
            var mapped = new AsyncApiServer
            {
                Host = server.Host,
                Description = server.Description,
                Protocol = server.Protocol,
                ProtocolVersion = server.ProtocolVersion,
                Tags = server.Tags.ToList(),
                Security = server.Security.ToList(),
            };

            foreach (var pair in server.Variables)
            {
                mapped.Variables[pair.Key] = new AsyncApiServerVariable
                {
                    Default = pair.Value.Default,
                    Description = pair.Value.Description,
                };
                foreach (var value in pair.Value.Enum)
                {
                    mapped.Variables[pair.Key].Enum.Add(value);
                }

                foreach (var example in pair.Value.Examples)
                {
                    mapped.Variables[pair.Key].Examples.Add(example);
                }
            }

            return mapped;
        }
    }
}
