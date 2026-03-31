using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Namotion.Reflection;
using Saunter.AttributeProvider.Attributes;
using Saunter.Options;
using Saunter.Options.Filters;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.AttributeProvider
{
    internal class AttributeDocumentProvider : IAsyncApiDocumentProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAsyncApiSchemaGenerator _schemaGenerator;
        private readonly IAsyncApiChannelUnion _channelUnion;
        private readonly IAsyncApiDocumentCloner _cloner;

        public AttributeDocumentProvider(IServiceProvider serviceProvider, IAsyncApiSchemaGenerator schemaGenerator, IAsyncApiChannelUnion channelUnion, IAsyncApiDocumentCloner cloner)
        {
            _serviceProvider = serviceProvider;
            _schemaGenerator = schemaGenerator;
            _channelUnion = channelUnion;
            _cloner = cloner;
        }

        public AsyncApiDocument GetDocument(string? documentName, AsyncApiOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var asyncApiTypes = GetAsyncApiTypes(options, documentName);
            var apiNamePair = options.NamedApis.FirstOrDefault(c => c.Value.Id == documentName);
            var clone = _cloner.CloneProtype(apiNamePair.Value ?? options.AsyncApi);

            clone.Asyncapi = "3.0.0";
            clone.DefaultContentType ??= "application/json";
            clone.Components ??= new AsyncApiComponents();
            clone.Channels ??= new Dictionary<string, AsyncApiChannel>();
            clone.Operations ??= new Dictionary<string, AsyncApiOperation>();

            var generatedItems = GenerateChannelsFromMethods(clone.Components, options, asyncApiTypes)
                .Concat(GenerateChannelsFromClasses(clone.Components, options, asyncApiTypes));

            foreach (var item in generatedItems)
            {
                if (!clone.Channels.TryAdd(item.ChannelId, item.Channel))
                {
                    clone.Channels[item.ChannelId] = _channelUnion.Union(clone.Channels[item.ChannelId], item.Channel);
                }

                if (!clone.Operations.TryAdd(item.OperationId, item.Operation))
                {
                    throw new InvalidOperationException($"Operation conflict for '{item.OperationId}'.");
                }
            }

            var filterContext = new DocumentFilterContext(asyncApiTypes);
            foreach (var filterType in options.DocumentFilters)
            {
                var filter = (IDocumentFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(clone, filterContext);
            }

            return clone;
        }

        private IEnumerable<GeneratedOperation> GenerateChannelsFromMethods(AsyncApiComponents components, AsyncApiOptions options, TypeInfo[] asyncApiTypes)
        {
            var methodsWithChannelAttribute = asyncApiTypes
                .SelectMany(type => type.DeclaredMethods)
                .Select(method => new
                {
                    Channel = method.GetCustomAttribute<ChannelAttribute>(),
                    Method = method,
                })
                .Where(mc => mc.Channel != null);

            foreach (var item in methodsWithChannelAttribute)
            {
                var channel = item.Channel!;
                var operationMessages = GetOperationAttributes(item.Method)
                    .ToDictionary(
                        operationAttribute => operationAttribute,
                        operationAttribute => ResolveMessagesForOperation(components, item.Method, operationAttribute));
                var channelItem = CreateChannel(channel, UnionMessageReferences(operationMessages.Values));
                PopulateChannelParameters(components, item.Method, channelItem);

                ApplyChannelFilters(options, item.Method, channel, channelItem);

                foreach (var pair in operationMessages)
                {
                    var operation = CreateOperation(item.Method, pair.Key, channel.ChannelId, pair.Value);
                    ApplyOperationFilters(item.Method, options, pair.Key, operation);

                    yield return new GeneratedOperation(
                        channel.ChannelId,
                        channelItem,
                        GetOperationId(pair.Key, item.Method, pair.Key.Action),
                        operation);
                }
            }
        }

        private IEnumerable<GeneratedOperation> GenerateChannelsFromClasses(AsyncApiComponents components, AsyncApiOptions options, TypeInfo[] asyncApiTypes)
        {
            var classesWithChannelAttribute = asyncApiTypes
                .Select(type => new
                {
                    Channel = type.GetCustomAttribute<ChannelAttribute>(),
                    Type = type,
                })
                .Where(cc => cc.Channel != null);

            foreach (var item in classesWithChannelAttribute)
            {
                var channel = item.Channel!;
                var operationMessages = GetOperationAttributes(item.Type)
                    .ToDictionary(
                        operationAttribute => operationAttribute,
                        operationAttribute => ResolveMessagesForOperation(components, item.Type, operationAttribute));
                var channelItem = CreateChannel(channel, UnionMessageReferences(operationMessages.Values));
                PopulateChannelParameters(components, item.Type, channelItem);

                ApplyChannelFilters(options, item.Type, channel, channelItem);

                foreach (var pair in operationMessages)
                {
                    var operation = CreateOperation(item.Type, pair.Key, channel.ChannelId, pair.Value);
                    ApplyOperationFilters(item.Type, options, pair.Key, operation);

                    yield return new GeneratedOperation(
                        channel.ChannelId,
                        channelItem,
                        GetOperationId(pair.Key, item.Type, pair.Key.Action),
                        operation);
                }
            }
        }

        private AsyncApiChannel CreateChannel(ChannelAttribute attribute, IReadOnlyList<AsyncApiMessageReference> messageReferences)
        {
            var channel = new AsyncApiChannel
            {
                Address = attribute.Address,
                Title = attribute.Title,
                Summary = attribute.Summary,
                Description = attribute.Description,
                Servers = attribute.Servers.Select(serverName => new AsyncApiServerReference($"#/servers/{serverName}")).ToList(),
                Messages = CreateChannelMessageMap(messageReferences),
                Parameters = new Dictionary<string, AsyncApiParameter>(),
                Tags = new List<AsyncApiTag>(),
                Extensions = new Dictionary<string, IAsyncApiExtension>(),
                Bindings = CreateBindingsReference<IChannelBinding>(attribute.BindingsRef, "channelBindings"),
            };

            return channel;
        }

        private void ApplyChannelFilters(AsyncApiOptions options, MemberInfo member, ChannelAttribute channel, AsyncApiChannel channelItem)
        {
            var context = new ChannelFilterContext(member, channel);

            foreach (var filterType in options.ChannelFilters)
            {
                var filter = (IChannelFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(channelItem, context);
            }
        }

        private AsyncApiOperation CreateOperation(MethodInfo method, OperationAttribute operationAttribute, string channelId, IReadOnlyList<AsyncApiMessageReference> messageReferences)
        {
            var operation = CreateOperationCore(method, operationAttribute, channelId, messageReferences);
            return operation;
        }

        private AsyncApiOperation CreateOperation(TypeInfo type, OperationAttribute operationAttribute, string channelId, IReadOnlyList<AsyncApiMessageReference> messageReferences)
        {
            var operation = CreateOperationCore(type, operationAttribute, channelId, messageReferences);
            return operation;
        }

        private AsyncApiOperation CreateOperationCore(MemberInfo member, OperationAttribute operationAttribute, string channelId, IReadOnlyList<AsyncApiMessageReference> messageReferences)
        {
            var tags = operationAttribute.Tags?.Select(tag => new AsyncApiTag { Name = tag }).Cast<AsyncApiTag>().ToList()
                ?? new List<AsyncApiTag>();

            return new AsyncApiOperation
            {
                Action = operationAttribute.Action,
                Channel = new AsyncApiChannelReference($"#/channels/{channelId}"),
                Title = operationAttribute.Title,
                Summary = operationAttribute.Summary ?? member.GetXmlDocsSummary(),
                Description = operationAttribute.Description ?? (member.GetXmlDocsRemarks() != string.Empty ? member.GetXmlDocsRemarks() : null),
                Messages = CreateOperationMessageReferences(channelId, messageReferences),
                Tags = tags,
                Security = new List<AsyncApiSecurityScheme>(),
                Traits = new List<AsyncApiOperationTrait>(),
                Extensions = new Dictionary<string, IAsyncApiExtension>(),
                Bindings = CreateBindingsReference<IOperationBinding>(operationAttribute.BindingsRef, "operationBindings"),
            };
        }

        private void ApplyOperationFilters(MemberInfo member, AsyncApiOptions options, OperationAttribute operationAttribute, AsyncApiOperation operation)
        {
            var filterContext = new OperationFilterContext(member, operationAttribute);

            foreach (var filterType in options.OperationFilters)
            {
                var filter = (IOperationFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(operation, filterContext);
            }
        }

        private IReadOnlyList<AsyncApiMessageReference> ResolveMessagesForOperation(AsyncApiComponents components, MethodInfo method, OperationAttribute operationAttribute)
        {
            var messageAttributes = method.GetCustomAttributes<MessageAttribute>().ToArray();
            if (messageAttributes.Any())
            {
                return GenerateMessagesFromAttributes(components, messageAttributes);
            }

            if (operationAttribute.MessagePayloadType is not null)
            {
                return GenerateMessageFromType(components, operationAttribute.MessagePayloadType.GetTypeInfo());
            }

            return Array.Empty<AsyncApiMessageReference>();
        }

        private IReadOnlyList<AsyncApiMessageReference> ResolveMessagesForOperation(AsyncApiComponents components, TypeInfo type, OperationAttribute operationAttribute)
        {
            var messageAttributes = type
                .DeclaredMethods
                .SelectMany(method => method.GetCustomAttributes<MessageAttribute>())
                .ToArray();

            if (messageAttributes.Any())
            {
                return GenerateMessagesFromAttributes(components, messageAttributes);
            }

            if (operationAttribute.MessagePayloadType is not null)
            {
                return GenerateMessageFromType(components, operationAttribute.MessagePayloadType.GetTypeInfo());
            }

            return Array.Empty<AsyncApiMessageReference>();
        }

        private IReadOnlyList<AsyncApiMessageReference> GenerateMessagesFromAttributes(AsyncApiComponents components, IEnumerable<MessageAttribute> messageAttributes)
        {
            return messageAttributes
                .Select(attribute => GenerateMessageFromAttribute(components, attribute))
                .Where(message => message != null)
                .Cast<AsyncApiMessageReference>()
                .ToList();
        }

        private List<AsyncApiMessageReference> GenerateMessageFromType(AsyncApiComponents components, TypeInfo payloadType)
        {
            var payloadSchema = GetAsyncApiSchemaReference(components, payloadType);
            var messageId = payloadSchema?.Id;

            if (string.IsNullOrWhiteSpace(messageId))
            {
                return new List<AsyncApiMessageReference>();
            }

            if (!components.Messages.ContainsKey(messageId))
            {
                components.Messages.Add(messageId, new AsyncApiMessage
                {
                    Name = messageId,
                    Title = messageId,
                    Payload = CreateSchemaWrapper(payloadSchema?.Schema),
                    Tags = new List<AsyncApiTag>(),
                    Examples = new List<AsyncApiMessageExample>(),
                    Traits = new List<AsyncApiMessageTrait>(),
                    Extensions = new Dictionary<string, IAsyncApiExtension>(),
                    Bindings = new AsyncApiBindings<IMessageBinding>(),
                });
            }

            return new List<AsyncApiMessageReference>
            {
                new AsyncApiMessageReference($"#/components/messages/{messageId}")
            };
        }

        private AsyncApiMessageReference? GenerateMessageFromAttribute(AsyncApiComponents components, MessageAttribute messageAttribute)
        {
            if (messageAttribute.PayloadType == null)
            {
                return null;
            }

            var payloadSchema = GetAsyncApiSchemaReference(components, messageAttribute.PayloadType.GetTypeInfo());
            var messageId = messageAttribute.MessageId ?? payloadSchema?.Id ?? messageAttribute.PayloadType.Name;

            if (!components.Messages.ContainsKey(messageId))
            {
                var headersSchema = GetAsyncApiSchemaReference(components, messageAttribute.HeadersType?.GetTypeInfo());
                var tags = messageAttribute.Tags?.Select(tag => new AsyncApiTag { Name = tag }).Cast<AsyncApiTag>().ToList()
                    ?? new List<AsyncApiTag>();

                components.Messages.Add(messageId, new AsyncApiMessage
                {
                    Name = messageAttribute.Name ?? payloadSchema?.Id ?? messageAttribute.PayloadType.Name,
                    Title = messageAttribute.Title ?? payloadSchema?.Id ?? messageAttribute.PayloadType.Name,
                    Summary = messageAttribute.Summary,
                    Description = messageAttribute.Description,
                    Payload = CreateSchemaWrapper(payloadSchema?.Schema),
                    Headers = CreateSchemaWrapper(headersSchema?.Schema),
                    Tags = tags,
                    Examples = new List<AsyncApiMessageExample>(),
                    Traits = new List<AsyncApiMessageTrait>(),
                    Extensions = new Dictionary<string, IAsyncApiExtension>(),
                    Bindings = CreateBindingsReference<IMessageBinding>(messageAttribute.BindingsRef, "messageBindings"),
                });
            }

            return new AsyncApiMessageReference($"#/components/messages/{messageId}");
        }

        private Dictionary<string, AsyncApiMessage> CreateChannelMessageMap(IReadOnlyList<AsyncApiMessageReference> messageReferences)
        {
            var messages = new Dictionary<string, AsyncApiMessage>();

            foreach (var messageReference in messageReferences.OfType<AsyncApiMessageReference>())
            {
                var messageId = GetReferenceKey(messageReference.Reference.Reference);
                messages[messageId] = messageReference;
            }

            return messages;
        }

        private List<AsyncApiMessageReference> CreateOperationMessageReferences(string channelId, IReadOnlyList<AsyncApiMessageReference> messageReferences)
        {
            return messageReferences
                .Select(messageReference => new AsyncApiMessageReference(
                    $"#/channels/{channelId}/messages/{GetReferenceKey(messageReference.Reference.Reference)}"))
                .ToList();
        }

        private static IReadOnlyList<AsyncApiMessageReference> UnionMessageReferences(IEnumerable<IReadOnlyList<AsyncApiMessageReference>> messageSets)
        {
            return messageSets
                .SelectMany(messages => messages)
                .DistinctBy(message => message.Reference.Reference)
                .ToList();
        }

        private IDictionary<string, AsyncApiParameter> GetChannelParametersFromAttributes(MemberInfo memberInfo)
        {
            var attributes = memberInfo.GetCustomAttributes<ChannelParameterAttribute>().ToArray();
            var parameters = new Dictionary<string, AsyncApiParameter>(attributes.Length);

            foreach (var attribute in attributes)
            {
                parameters.Add(attribute.Name, CreateChannelParameter(attribute));
            }

            return parameters;
        }

        private AsyncApiParameter CreateChannelParameter(ChannelParameterAttribute attribute)
        {
            var parameter = new AsyncApiParameter
            {
                Description = attribute.Description,
                Location = attribute.Location,
                Examples = new List<string>(),
                Enum = new List<string>(),
                Extensions = new Dictionary<string, IAsyncApiExtension>(),
            };

            if (attribute.Type.IsEnum)
            {
                foreach (var value in Enum.GetNames(attribute.Type))
                {
                    parameter.Enum.Add(value);
                }
            }

            return parameter;
        }

        private void PopulateChannelParameters(AsyncApiComponents components, MemberInfo member, AsyncApiChannel channel)
        {
            var parameters = GetChannelParametersFromAttributes(member);
            foreach (var pair in parameters)
            {
                if (!components.Parameters.ContainsKey(pair.Key))
                {
                    components.Parameters.Add(pair.Key, pair.Value);
                }

                channel.Parameters[pair.Key] = new AsyncApiParameterReference($"#/components/parameters/{pair.Key}");
            }
        }

        private SchemaReferenceInfo? GetAsyncApiSchemaReference(AsyncApiComponents components, TypeInfo? payloadType)
        {
            var generatedSchemas = _schemaGenerator.Generate(payloadType?.AsType());
            if (generatedSchemas is null)
            {
                return null;
            }

            foreach (var asyncApiSchema in generatedSchemas.Value.All.Where(schema => schema is not AsyncApiJsonSchemaReference))
            {
                if (!string.IsNullOrWhiteSpace(asyncApiSchema.Title) && !components.Schemas.ContainsKey(asyncApiSchema.Title))
                {
                    components.Schemas[asyncApiSchema.Title] = asyncApiSchema;
                }
            }

            return new SchemaReferenceInfo(
                generatedSchemas.Value.Root.Title,
                new AsyncApiJsonSchemaReference($"#/components/schemas/{generatedSchemas.Value.Root.Title}"));
        }

        private static AsyncApiMultiFormatSchema? CreateSchemaWrapper(AsyncApiJsonSchema? schema)
        {
            if (schema is null)
            {
                return null;
            }

            return new AsyncApiMultiFormatSchema
            {
                Schema = schema,
            };
        }

        private static IEnumerable<OperationAttribute> GetOperationAttributes(MemberInfo member)
        {
            var send = member.GetCustomAttribute<SendOperationAttribute>();
            if (send != null)
            {
                yield return send;
            }

            var receive = member.GetCustomAttribute<ReceiveOperationAttribute>();
            if (receive != null)
            {
                yield return receive;
            }
        }

        private static string GetOperationId(OperationAttribute attribute, MemberInfo member, AsyncApiAction action)
        {
            if (!string.IsNullOrWhiteSpace(attribute.OperationId))
            {
                return attribute.OperationId;
            }

            return $"{member.DeclaringType?.Name ?? member.Name}.{member.Name}.{action.ToString().ToLowerInvariant()}";
        }

        private static string GetReferenceKey(string reference)
        {
            var segments = reference.Split('/');
            return segments[^1];
        }

        private static AsyncApiBindings<TBinding> CreateBindingsReference<TBinding>(string? bindingsRef, string componentName)
            where TBinding : IBinding
        {
            if (string.IsNullOrWhiteSpace(bindingsRef))
            {
                return new AsyncApiBindings<TBinding>();
            }

            return new AsyncApiBindingsReference<TBinding>($"#/components/{componentName}/{bindingsRef}");
        }

        private static TypeInfo[] GetAsyncApiTypes(AsyncApiOptions options, string? apiName)
        {
            return options
                .AsyncApiSchemaTypes
                .Where(t => t.GetCustomAttribute<AsyncApiAttribute>()?.DocumentName == apiName)
                .ToArray();
        }

        private readonly record struct GeneratedOperation(string ChannelId, AsyncApiChannel Channel, string OperationId, AsyncApiOperation Operation);
        private readonly record struct SchemaReferenceInfo(string Id, AsyncApiJsonSchema Schema);
    }
}
