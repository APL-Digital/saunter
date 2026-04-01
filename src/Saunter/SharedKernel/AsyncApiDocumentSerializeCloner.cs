using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Microsoft.Extensions.Logging;
using Saunter.AttributeProvider.Descriptors;
using Saunter.SharedKernel.Descriptors;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiDocumentSerializeCloner : IAsyncApiDocumentCloner
    {
        public AsyncApiDocumentSerializeCloner()
        {
        }

        public AsyncApiDocumentSerializeCloner(ILogger<AsyncApiDocumentSerializeCloner> _, IAsyncApiDocumentWriter? __ = null)
        {
        }

        public AsyncApiDocumentDescriptor ClonePrototype(AsyncApiDocumentDescriptor prototype)
        {
            return new AsyncApiDocumentDescriptor
            {
                Id = prototype.Id,
                Asyncapi = prototype.Asyncapi,
                DefaultContentType = prototype.DefaultContentType,
                Info = CloneInfo(prototype.Info),
                Components = CloneComponents(prototype.Components),
                Servers = prototype.Servers.ToDictionary(pair => pair.Key, pair => CloneServer(pair.Value)),
                Channels = prototype.Channels.ToDictionary(pair => pair.Key, pair => CloneChannel(pair.Value)),
                Operations = prototype.Operations.ToDictionary(pair => pair.Key, pair => CloneOperation(pair.Value)),
            };
        }

        private static AsyncApiInfoDescriptor CloneInfo(AsyncApiInfoDescriptor info)
        {
            return new AsyncApiInfoDescriptor
            {
                Version = info.Version,
                Title = info.Title,
                Description = info.Description,
                Contact = info.Contact is null ? null : new AsyncApiContactDescriptor
                {
                    Url = info.Contact.Url,
                    Name = info.Contact.Name,
                    Email = info.Contact.Email,
                },
                License = info.License is null ? null : new AsyncApiLicenseDescriptor
                {
                    Url = info.License.Url,
                    Name = info.License.Name,
                },
                TermsOfService = info.TermsOfService,
            };
        }

        private static AsyncApiComponentsDescriptor CloneComponents(AsyncApiComponentsDescriptor components)
        {
            return new AsyncApiComponentsDescriptor
            {
                Schemas = components.Schemas.ToDictionary(pair => pair.Key, pair => CloneSchema(pair.Value)),
                Messages = components.Messages.ToDictionary(pair => pair.Key, pair => pair.Value with { Tags = pair.Value.Tags.ToArray() }),
                Parameters = components.Parameters.ToDictionary(pair => pair.Key, pair => pair.Value with { EnumValues = pair.Value.EnumValues.ToArray(), Examples = pair.Value.Examples.ToArray() }),
                OperationBindings = components.OperationBindings.ToDictionary(pair => pair.Key, pair => CloneBindings(pair.Value)),
                MessageBindings = components.MessageBindings.ToDictionary(pair => pair.Key, pair => CloneBindings(pair.Value)),
                ChannelBindings = components.ChannelBindings.ToDictionary(pair => pair.Key, pair => CloneBindings(pair.Value)),
                OperationTraits = new Dictionary<string, AsyncApiOperationTrait>(components.OperationTraits),
                CorrelationIds = new Dictionary<string, AsyncApiCorrelationId>(components.CorrelationIds),
                SecuritySchemes = new Dictionary<string, AsyncApiSecurityScheme>(components.SecuritySchemes),
            };
        }

        private static AsyncApiServerDescriptor CloneServer(AsyncApiServerDescriptor server)
        {
            return new AsyncApiServerDescriptor
            {
                Host = server.Host,
                Description = server.Description,
                Protocol = server.Protocol,
                ProtocolVersion = server.ProtocolVersion,
                Tags = server.Tags.ToList(),
                Security = server.Security.ToList(),
                Variables = server.Variables.ToDictionary(
                    pair => pair.Key,
                    pair => new AsyncApiServerVariableDescriptor
                    {
                        Default = pair.Value.Default,
                        Description = pair.Value.Description,
                        Enum = pair.Value.Enum.ToList(),
                        Examples = pair.Value.Examples.ToList(),
                    }),
            };
        }

        private static AsyncApiChannelDescriptor CloneChannel(AsyncApiChannelDescriptor channel)
        {
            return channel with
            {
                ServerNames = channel.ServerNames.ToArray(),
                MessageIds = channel.MessageIds.ToArray(),
                Parameters = channel.Parameters.Select(parameter => parameter with { EnumValues = parameter.EnumValues.ToArray(), Examples = parameter.Examples.ToArray() }).ToArray(),
            };
        }

        private static AsyncApiOperationDescriptor CloneOperation(AsyncApiOperationDescriptor operation)
        {
            var clone = operation with
            {
                MessageIds = operation.MessageIds.ToArray(),
                Tags = operation.Tags.ToArray(),
            };

            foreach (var traitReference in operation.TraitReferences)
            {
                clone.TraitReferences.Add(traitReference);
            }

            return clone;
        }

        private static AsyncApiSchemaDescriptor CloneSchema(AsyncApiSchemaDescriptor schema)
        {
            var clone = new AsyncApiSchemaDescriptor
            {
                Id = schema.Id,
                Type = schema.Type,
                Format = schema.Format,
                Nullable = schema.Nullable,
                Reference = schema.Reference,
                Items = schema.Items is null ? null : CloneSchema(schema.Items),
            };

            foreach (var pair in schema.Properties)
            {
                clone.Properties[pair.Key] = CloneSchema(pair.Value);
            }

            foreach (var required in schema.Required)
            {
                clone.Required.Add(required);
            }

            foreach (var value in schema.EnumValues)
            {
                clone.EnumValues.Add(value);
            }

            foreach (var item in schema.OneOf)
            {
                clone.OneOf.Add(CloneSchema(item));
            }

            foreach (var item in schema.AllOf)
            {
                clone.AllOf.Add(CloneSchema(item));
            }

            return clone;
        }

        private static AsyncApiBindings<TBinding> CloneBindings<TBinding>(AsyncApiBindings<TBinding> bindings)
            where TBinding : IBinding
        {
            if (bindings.GetType() != typeof(AsyncApiBindings<TBinding>))
            {
                return bindings;
            }

            var clone = new AsyncApiBindings<TBinding>();
            foreach (var pair in bindings)
            {
                clone[pair.Key] = pair.Value;
            }

            return clone;
        }
    }
}
