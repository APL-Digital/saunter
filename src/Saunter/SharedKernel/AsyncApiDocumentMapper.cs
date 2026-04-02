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
            var components = document.Components ?? new AsyncApiComponentsDescriptor();
            var messages = components.Messages ?? new Dictionary<string, AsyncApiMessageDescriptor>();
            var schemas = components.Schemas ?? new Dictionary<string, SharedKernel.Descriptors.AsyncApiSchemaDescriptor>();
            var mapped = new AsyncApiDocument
            {
                Id = document.Id,
                Asyncapi = document.Asyncapi,
                DefaultContentType = document.DefaultContentType,
                Info = MapInfo(document.Info),
                Components = new AsyncApiComponents
                {
                    Schemas = new Dictionary<string, AsyncApiMultiFormatSchema>(),
                    Messages = new Dictionary<string, AsyncApiMessage>(),
                    Parameters = new Dictionary<string, AsyncApiParameter>(),
                    OperationBindings = new Dictionary<string, AsyncApiBindings<IOperationBinding>>(components.OperationBindings ?? new Dictionary<string, AsyncApiBindings<IOperationBinding>>()),
                    MessageBindings = new Dictionary<string, AsyncApiBindings<IMessageBinding>>(components.MessageBindings ?? new Dictionary<string, AsyncApiBindings<IMessageBinding>>()),
                    ChannelBindings = new Dictionary<string, AsyncApiBindings<IChannelBinding>>(components.ChannelBindings ?? new Dictionary<string, AsyncApiBindings<IChannelBinding>>()),
                    OperationTraits = new Dictionary<string, AsyncApiOperationTrait>(components.OperationTraits ?? new Dictionary<string, AsyncApiOperationTrait>()),
                    CorrelationIds = new Dictionary<string, AsyncApiCorrelationId>(components.CorrelationIds ?? new Dictionary<string, AsyncApiCorrelationId>()),
                    SecuritySchemes = new Dictionary<string, AsyncApiSecurityScheme>(components.SecuritySchemes ?? new Dictionary<string, AsyncApiSecurityScheme>()),
                },
                Servers = document.Servers.ToDictionary(pair => pair.Key, pair => MapServer(pair.Value)),
                Channels = new Dictionary<string, AsyncApiChannel>(),
                Operations = new Dictionary<string, AsyncApiOperation>(),
            };

            var messageResolution = new AsyncApiMessageResolutionDescriptor(
                messages.Keys.ToArray(),
                messages.Values.ToArray(),
                schemas.Select(pair => new AsyncApiSchemaComponentDescriptor(pair.Key, pair.Value)).ToArray());
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
