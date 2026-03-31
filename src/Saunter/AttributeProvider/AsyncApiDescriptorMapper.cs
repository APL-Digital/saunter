using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Saunter.AttributeProvider.Descriptors;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.AttributeProvider
{
    internal class AsyncApiDescriptorMapper : IAsyncApiDescriptorMapper
    {
        private readonly IAsyncApiSchemaMapper _schemaMapper;

        public AsyncApiDescriptorMapper(IAsyncApiSchemaMapper schemaMapper)
        {
            _schemaMapper = schemaMapper;
        }

        public void RegisterMessageResolution(AsyncApiComponents components, AsyncApiMessageResolutionDescriptor resolution)
        {
            foreach (var schema in resolution.Schemas)
            {
                if (!components.Schemas.ContainsKey(schema.Id))
                {
                    components.Schemas[schema.Id] = _schemaMapper.Map(schema.Schema);
                }
            }

            foreach (var message in resolution.Messages)
            {
                if (components.Messages.ContainsKey(message.Id))
                {
                    continue;
                }

                components.Messages[message.Id] = new AsyncApiMessage
                {
                    Name = message.Name,
                    Title = message.Title,
                    Summary = message.Summary,
                    Description = message.Description,
                    Payload = CreateSchemaWrapper(message.PayloadSchemaId),
                    Headers = CreateSchemaWrapper(message.HeadersSchemaId),
                    CorrelationId = CreateCorrelationIdReference(message.CorrelationIdRef),
                    ContentType = message.ContentType,
                    Tags = message.Tags.Select(tag => new AsyncApiTag { Name = tag }).Cast<AsyncApiTag>().ToList(),
                    ExternalDocs = AttributeProviderModelFactory.CreateExternalDocs(message.ExternalDocsUrl, message.ExternalDocsDescription),
                    Examples = new List<AsyncApiMessageExample>(),
                    Traits = new List<AsyncApiMessageTrait>(),
                    Extensions = new Dictionary<string, IAsyncApiExtension>(),
                    Bindings = AttributeProviderModelFactory.CreateBindingsReference<IMessageBinding>(message.BindingsRef, "messageBindings"),
                };
            }
        }

        public AsyncApiChannel MapChannel(AsyncApiComponents components, AsyncApiChannelDescriptor descriptor)
        {
            foreach (var parameter in descriptor.Parameters)
            {
                if (!components.Parameters.ContainsKey(parameter.Name))
                {
                    components.Parameters[parameter.Name] = new AsyncApiParameter
                    {
                        Description = parameter.Description,
                        Location = parameter.Location,
                        Default = parameter.DefaultValue,
                        Examples = parameter.Examples.ToList(),
                        Enum = parameter.EnumValues.ToList(),
                        Extensions = new Dictionary<string, IAsyncApiExtension>(),
                    };
                }
            }

            return new AsyncApiChannel
            {
                Address = descriptor.Address,
                Title = descriptor.Title,
                Summary = descriptor.Summary,
                Description = descriptor.Description,
                Servers = descriptor.ServerNames.Select(serverName => new AsyncApiServerReference($"#/servers/{serverName}")).ToList(),
                Messages = descriptor.MessageIds.ToDictionary(
                    messageId => messageId,
                    messageId => (AsyncApiMessage)new AsyncApiMessageReference($"#/components/messages/{messageId}")),
                Parameters = descriptor.Parameters.ToDictionary(
                    parameter => parameter.Name,
                    parameter => (AsyncApiParameter)new AsyncApiParameterReference($"#/components/parameters/{parameter.Name}")),
                Tags = descriptor.Tags.ToList(),
                Extensions = new Dictionary<string, IAsyncApiExtension>(),
                Bindings = AttributeProviderModelFactory.CreateBindingsReference<IChannelBinding>(descriptor.BindingsRef, "channelBindings"),
            };
        }

        public AsyncApiOperation MapOperation(AsyncApiOperationDescriptor descriptor)
        {
            var operation = new AsyncApiOperation
            {
                Action = descriptor.Action,
                Channel = new AsyncApiChannelReference($"#/channels/{descriptor.ChannelId}"),
                Title = descriptor.Title,
                Summary = descriptor.Summary,
                Description = descriptor.Description,
                Messages = descriptor.MessageIds
                    .Select(messageId => new AsyncApiMessageReference($"#/channels/{descriptor.ChannelId}/messages/{messageId}"))
                    .ToList(),
                Tags = descriptor.Tags.Select(tag => new AsyncApiTag { Name = tag }).Cast<AsyncApiTag>().ToList(),
                Security = new List<AsyncApiSecurityScheme>(),
                Traits = new List<AsyncApiOperationTrait>(),
                Reply = CreateReply(descriptor.Reply),
                Extensions = new Dictionary<string, IAsyncApiExtension>(),
                Bindings = AttributeProviderModelFactory.CreateBindingsReference<IOperationBinding>(descriptor.BindingsRef, "operationBindings"),
            };

            foreach (var traitReference in descriptor.TraitReferences)
            {
                operation.Traits.Add(new AsyncApiOperationTraitReference($"#/components/operationTraits/{traitReference}"));
            }

            return operation;
        }

        private static AsyncApiMultiFormatSchema? CreateSchemaWrapper(string? schemaId)
        {
            if (string.IsNullOrWhiteSpace(schemaId))
            {
                return null;
            }

            return AttributeProviderModelFactory.CreateSchemaWrapper(new AsyncApiJsonSchemaReference($"#/components/schemas/{schemaId}"));
        }

        private static AsyncApiCorrelationId? CreateCorrelationIdReference(string? correlationIdRef)
        {
            if (string.IsNullOrWhiteSpace(correlationIdRef))
            {
                return null;
            }

            return new AsyncApiCorrelationIdReference($"#/components/correlationIds/{correlationIdRef}");
        }

        private static AsyncApiOperationReply? CreateReply(AsyncApiOperationReplyDescriptor? reply)
        {
            if (reply is null)
            {
                return null;
            }

            return new AsyncApiOperationReply
            {
                Channel = string.IsNullOrWhiteSpace(reply.ChannelId)
                    ? null
                    : new AsyncApiChannelReference($"#/channels/{reply.ChannelId}"),
                Address = string.IsNullOrWhiteSpace(reply.AddressLocation)
                    ? null
                    : new AsyncApiOperationReplyAddress
                    {
                        Description = reply.AddressDescription,
                        Location = reply.AddressLocation,
                    },
                Messages = new List<AsyncApiMessageReference>(),
                Extensions = new Dictionary<string, IAsyncApiExtension>(),
            };
        }
    }
}
