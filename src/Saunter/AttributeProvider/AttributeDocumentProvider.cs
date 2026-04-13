using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ByteBard.AsyncAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options;
using Saunter.Options.Filters;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.AttributeProvider
{
    internal class AttributeDocumentProvider : IAsyncApiDocumentProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAttributeMessageResolver _messageResolver;
        private readonly IAttributeChannelBuilder _channelBuilder;
        private readonly IAttributeOperationBuilder _operationBuilder;
        private readonly IAsyncApiChannelUnion _channelUnion;
        private readonly IAsyncApiDocumentCloner _cloner;
        private readonly IAsyncApiDocumentValidator _documentValidator;

        public AttributeDocumentProvider(
            IServiceProvider serviceProvider,
            IAttributeMessageResolver messageResolver,
            IAttributeChannelBuilder channelBuilder,
            IAttributeOperationBuilder operationBuilder,
            IAsyncApiChannelUnion channelUnion,
            IAsyncApiDocumentCloner cloner,
            IAsyncApiDocumentValidator documentValidator)
        {
            _serviceProvider = serviceProvider;
            _messageResolver = messageResolver;
            _channelBuilder = channelBuilder;
            _operationBuilder = operationBuilder;
            _channelUnion = channelUnion;
            _cloner = cloner;
            _documentValidator = documentValidator;
        }

        public AsyncApiDocumentDescriptor GetDocument(string? documentName, AsyncApiOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var asyncApiTypes = GetAsyncApiTypes(options, documentName);
            var sourceDocument = documentName is not null && options.NamedApis.TryGetValue(documentName, out var namedDocument)
                ? namedDocument
                : options.AsyncApi;
            var clone = _cloner.ClonePrototype(sourceDocument);

            clone.Asyncapi = sourceDocument.Asyncapi?.StartsWith("2.") == true
                ? sourceDocument.Asyncapi
                : "3.0.0";
            if (options.Inference.AutoSetDefaultContentType)
            {
                clone.DefaultContentType ??= "application/json";
            }
            clone.Components ??= new AsyncApiComponentsDescriptor();
            clone.Channels ??= new Dictionary<string, AsyncApiChannelDescriptor>();
            clone.Operations ??= new Dictionary<string, AsyncApiOperationDescriptor>();
            clone.Servers ??= new Dictionary<string, AsyncApiServerDescriptor>();

            var generatedItems = GenerateChannelsFromMethods(clone.Components, options, asyncApiTypes)
                .Concat(GenerateChannelsFromClasses(clone.Components, options, asyncApiTypes));
            var operationSources = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var item in generatedItems)
            {
                if (!clone.Channels.TryAdd(item.ChannelId, item.Channel))
                {
                    clone.Channels[item.ChannelId] = _channelUnion.Union(clone.Channels[item.ChannelId], item.Channel);
                }

                if (item.OperationId is null || item.Operation is null)
                {
                    continue;
                }

                if (!clone.Operations.TryAdd(item.OperationId, item.Operation))
                {
                    var existingOperation = clone.Operations[item.OperationId];
                    var existingSource = operationSources.TryGetValue(item.OperationId, out var knownSource)
                        ? knownSource
                        : "preconfigured document operation";
                    throw new InvalidOperationException(
                        $"Operation id '{item.OperationId}' is produced by multiple operations. " +
                        $"Existing definition: source='{existingSource}', action='{existingOperation.Action}', channel='{existingOperation.ChannelId}', messages={FormatValues(existingOperation.MessageIds)}. " +
                        $"Incoming definition: source='{FormatMember(item.SourceMember)}', action='{item.Operation.Action}', channel='{item.Operation.ChannelId}', messages={FormatValues(item.Operation.MessageIds)}. " +
                        "Set an explicit OperationId or adjust inference so each operation id is unique.");
                }
                else
                {
                    operationSources[item.OperationId] = FormatMember(item.SourceMember);
                }
            }

            var filterContext = new DocumentFilterContext(asyncApiTypes);
            foreach (var filterType in options.DocumentFilters)
            {
                var filter = ResolveFilter<IDocumentFilter>(filterType);
                filter.Apply(clone, filterContext);
            }

            _documentValidator.Validate(clone);
            return clone;
        }

        private IEnumerable<GeneratedOperation> GenerateChannelsFromMethods(AsyncApiComponentsDescriptor components, AsyncApiOptions options, TypeInfo[] asyncApiTypes)
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
                var operationAttributes = GetOperationAttributes(item.Method).ToArray();
                var operationMessages = operationAttributes
                    .ToDictionary(
                        operationAttribute => operationAttribute,
                        operationAttribute => _messageResolver.ResolveForOperation(item.Method, operationAttribute, options.Inference));
                var replyMessages = operationAttributes
                    .ToDictionary(
                        operationAttribute => operationAttribute,
                        operationAttribute => _messageResolver.ResolveReplyForOperation(item.Method, operationAttribute, options.Inference));

                RegisterMessageResolutions(components, operationMessages.Values.Concat(replyMessages.Values));
                var channelItem = _channelBuilder.Build(item.Method, channel, UnionMessageIds(operationMessages.Values), options.Inference);
                RegisterChannelParameters(components, channelItem);

                ApplyChannelFilters(options, item.Method, channel, channelItem);

                foreach (var pair in operationMessages)
                {
                    var replyResolution = replyMessages[pair.Key];
                    ValidateReplyConfiguration(item.Method, pair.Key);
                    var replyMessageIds = GetReplyMessageIds(pair.Key, pair.Value, replyResolution);
                    var operation = _operationBuilder.Build(item.Method, pair.Key, channelItem.Id, pair.Value.MessageIds, replyMessageIds);
                    ApplyOperationFilters(item.Method, options, pair.Key, operation);

                    yield return new GeneratedOperation(
                        channelItem.Id,
                        channelItem,
                        GetOperationId(pair.Key, item.Method, pair.Key.Action, options),
                        operation,
                        item.Method);

                    if (TryCreateReplyChannel(channelItem, pair.Key, operation, out var replyChannel))
                    {
                        ApplyChannelFilters(options, item.Method, channel, replyChannel);

                        yield return new GeneratedOperation(
                            replyChannel.Id,
                            replyChannel,
                            null,
                            null,
                            item.Method);
                    }
                }
            }
        }

        private IEnumerable<GeneratedOperation> GenerateChannelsFromClasses(AsyncApiComponentsDescriptor components, AsyncApiOptions options, TypeInfo[] asyncApiTypes)
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
                var operationAttributes = GetOperationAttributes(item.Type).ToArray();
                var operationMessages = operationAttributes
                    .ToDictionary(
                        operationAttribute => operationAttribute,
                        operationAttribute => _messageResolver.ResolveForOperation(item.Type, operationAttribute, options.Inference));
                var replyMessages = operationAttributes
                    .ToDictionary(
                        operationAttribute => operationAttribute,
                        operationAttribute => _messageResolver.ResolveReplyForOperation(item.Type, operationAttribute, options.Inference));

                RegisterMessageResolutions(components, operationMessages.Values.Concat(replyMessages.Values));
                var channelItem = _channelBuilder.Build(item.Type, channel, UnionMessageIds(operationMessages.Values), options.Inference);
                RegisterChannelParameters(components, channelItem);

                ApplyChannelFilters(options, item.Type, channel, channelItem);

                foreach (var pair in operationMessages)
                {
                    var replyResolution = replyMessages[pair.Key];
                    ValidateReplyConfiguration(item.Type, pair.Key);
                    var replyMessageIds = GetReplyMessageIds(pair.Key, pair.Value, replyResolution);
                    var operation = _operationBuilder.Build(item.Type, pair.Key, channelItem.Id, pair.Value.MessageIds, replyMessageIds);
                    ApplyOperationFilters(item.Type, options, pair.Key, operation);

                    yield return new GeneratedOperation(
                        channelItem.Id,
                        channelItem,
                        GetOperationId(pair.Key, item.Type, pair.Key.Action, options),
                        operation,
                        item.Type);

                    if (TryCreateReplyChannel(channelItem, pair.Key, operation, out var replyChannel))
                    {
                        ApplyChannelFilters(options, item.Type, channel, replyChannel);

                        yield return new GeneratedOperation(
                            replyChannel.Id,
                            replyChannel,
                            null,
                            null,
                            item.Type);
                    }
                }
            }
        }

        private void ApplyChannelFilters(AsyncApiOptions options, MemberInfo member, ChannelAttribute channel, AsyncApiChannelDescriptor channelItem)
        {
            var context = new ChannelFilterContext(member, channel);

            foreach (var filterType in options.ChannelFilters)
            {
                var filter = ResolveFilter<IChannelFilter>(filterType);
                filter.Apply(channelItem, context);
            }
        }

        private void ApplyOperationFilters(MemberInfo member, AsyncApiOptions options, OperationAttribute operationAttribute, AsyncApiOperationDescriptor operation)
        {
            var filterContext = new OperationFilterContext(member, operationAttribute);

            foreach (var filterType in options.OperationFilters)
            {
                var filter = ResolveFilter<IOperationFilter>(filterType);
                filter.Apply(operation, filterContext);
            }
        }

        private TFilter ResolveFilter<TFilter>(Type filterType)
            where TFilter : class
        {
            return _serviceProvider.GetService(filterType) as TFilter
                ?? (TFilter)ActivatorUtilities.CreateInstance(_serviceProvider, filterType);
        }

        private void RegisterMessageResolutions(AsyncApiComponentsDescriptor components, IEnumerable<AsyncApiMessageResolutionDescriptor> resolutions)
        {
            foreach (var resolution in resolutions)
            {
                foreach (var schema in resolution.Schemas)
                {
                    if (!components.Schemas.ContainsKey(schema.Id))
                    {
                        components.Schemas[schema.Id] = schema.Schema;
                    }
                }

                foreach (var message in resolution.Messages)
                {
                    if (!components.Messages.ContainsKey(message.Id))
                    {
                        components.Messages[message.Id] = message;
                    }
                }
            }
        }

        private static void RegisterChannelParameters(AsyncApiComponentsDescriptor components, AsyncApiChannelDescriptor channel)
        {
            foreach (var parameter in channel.Parameters)
            {
                if (!components.Parameters.ContainsKey(parameter.Name))
                {
                    components.Parameters[parameter.Name] = parameter;
                }
            }
        }

        private static IReadOnlyList<string> UnionMessageIds(IEnumerable<AsyncApiMessageResolutionDescriptor> resolutions)
        {
            return resolutions
                .SelectMany(resolution => resolution.MessageIds)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static IReadOnlyList<string> GetReplyMessageIds(
            OperationAttribute operationAttribute,
            AsyncApiMessageResolutionDescriptor operationResolution,
            AsyncApiMessageResolutionDescriptor replyResolution)
        {
            if (string.IsNullOrWhiteSpace(operationAttribute.Reply))
            {
                return Array.Empty<string>();
            }

            return replyResolution.MessageIds.Count > 0
                ? replyResolution.MessageIds
                : operationResolution.MessageIds;
        }

        private static void ValidateReplyConfiguration(MemberInfo member, OperationAttribute operationAttribute)
        {
            if (!string.IsNullOrWhiteSpace(operationAttribute.ReplyChannelAddress)
                && !string.IsNullOrWhiteSpace(operationAttribute.ReplyAddressLocation))
            {
                throw new InvalidOperationException(
                    $"Operation '{FormatMember(member)}' configures both ReplyChannelAddress and ReplyAddressLocation, but those settings are mutually exclusive. Remove one of them so the reply channel is either explicitly addressed or dynamically addressed.");
            }

            if (!string.IsNullOrWhiteSpace(operationAttribute.Reply))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(operationAttribute.ReplyChannelAddress))
            {
                throw new InvalidOperationException(
                    $"Operation '{FormatMember(member)}' configures ReplyChannelAddress but no Reply channel id. Set OperationAttribute.Reply to the generated reply channel id.");
            }

            if (!string.IsNullOrWhiteSpace(operationAttribute.ReplyAddressLocation))
            {
                throw new InvalidOperationException(
                    $"Operation '{FormatMember(member)}' configures ReplyAddressLocation but no Reply channel id. Set OperationAttribute.Reply to the reply channel id or remove the reply address metadata.");
            }

            if (operationAttribute.ReplyMessagePayloadType is not null)
            {
                throw new InvalidOperationException(
                    $"Operation '{FormatMember(member)}' configures ReplyMessagePayloadType but no Reply channel id. Set OperationAttribute.Reply to the reply channel id or remove the reply payload type.");
            }
        }

        private static bool TryCreateReplyChannel(
            AsyncApiChannelDescriptor sourceChannel,
            OperationAttribute operationAttribute,
            AsyncApiOperationDescriptor operation,
            out AsyncApiChannelDescriptor replyChannel)
        {
            if (string.IsNullOrWhiteSpace(operation.Reply?.ChannelId))
            {
                replyChannel = default!;
                return false;
            }

            var replyMessageIds = operation.Reply.MessageIds;
            string? replyChannelAddress = null;
            if (string.IsNullOrWhiteSpace(operationAttribute.ReplyChannelAddress)
                && string.IsNullOrWhiteSpace(operation.Reply.AddressLocation))
            {
                if (replyMessageIds.Count == 0)
                {
                    replyChannel = default!;
                    return false;
                }
            }
            else if (string.IsNullOrWhiteSpace(operation.Reply.AddressLocation))
            {
                replyChannelAddress = operationAttribute.ReplyChannelAddress;
            }

            replyChannel = new AsyncApiChannelDescriptor(
                operation.Reply.ChannelId,
                replyChannelAddress,
                null,
                null,
                null,
                sourceChannel.BindingsRef,
                sourceChannel.ServerNames,
                replyMessageIds.ToArray(),
                Array.Empty<AsyncApiParameterDescriptor>());
            return true;
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

        private static string GetOperationId(OperationAttribute attribute, MemberInfo member, AsyncApiAction action, AsyncApiOptions options)
        {
            if (!string.IsNullOrWhiteSpace(attribute.OperationId))
            {
                return attribute.OperationId;
            }

            if (options.Inference.InferOperationIdFromMemberName)
            {
                return options.Inference.OperationIdGenerator(member, action);
            }

            return $"{member.DeclaringType?.Name ?? member.Name}.{member.Name}.{action.ToString().ToLowerInvariant()}";
        }

        private static TypeInfo[] GetAsyncApiTypes(AsyncApiOptions options, string? apiName)
        {
            return options
                .AsyncApiSchemaTypes
                .Where(t => t.GetCustomAttribute<AsyncApiAttribute>()?.DocumentName == apiName)
                .ToArray();
        }

        private static string FormatMember(MemberInfo member)
        {
            if (member is Type type)
            {
                return type.FullName ?? type.Name;
            }

            return member.DeclaringType is null
                ? member.Name
                : $"{member.DeclaringType.FullName}.{member.Name}";
        }

        private static string FormatValues(IReadOnlyList<string> values)
        {
            return values.Count == 0
                ? "[]"
                : $"[{string.Join(", ", values.Select(value => $"'{value}'"))}]";
        }

        private readonly record struct GeneratedOperation(string ChannelId, AsyncApiChannelDescriptor Channel, string? OperationId, AsyncApiOperationDescriptor? Operation, MemberInfo SourceMember);
    }
}
