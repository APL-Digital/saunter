using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using ByteBard.AsyncAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AttributeDocumentProvider> _logger;

        public AttributeDocumentProvider(
            IServiceProvider serviceProvider,
            IAttributeMessageResolver messageResolver,
            IAttributeChannelBuilder channelBuilder,
            IAttributeOperationBuilder operationBuilder,
            IAsyncApiChannelUnion channelUnion,
            IAsyncApiDocumentCloner cloner,
            IAsyncApiDocumentValidator documentValidator,
            ILogger<AttributeDocumentProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _messageResolver = messageResolver;
            _channelBuilder = channelBuilder;
            _operationBuilder = operationBuilder;
            _channelUnion = channelUnion;
            _cloner = cloner;
            _documentValidator = documentValidator;
            _logger = logger;
        }

        public AsyncApiDocumentDescriptor GetDocument(string? documentName, AsyncApiOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var asyncApiTypes = GetAsyncApiTypes(options, documentName);
            var isConfigured = TryGetConfiguredDocument(options, documentName, out var configuredDocument);
            if (documentName is not null && !isConfigured && asyncApiTypes.Length == 0)
            {
                var knownNames = options.Documents.Keys.Union(options.NamedApis.Keys).OrderBy(name => name, StringComparer.Ordinal).ToArray();
                throw new InvalidOperationException(
                    $"No AsyncAPI document named '{documentName}' is configured and no types are marked [AsyncApi(\"{documentName}\")]. " +
                    $"Known documents: {(knownNames.Length == 0 ? "<none>" : string.Join(", ", knownNames.Select(name => $"'{name}'")))}. " +
                    "Register the document with ConfigureAsyncApiDocument or annotate types with the matching document name.");
            }

            var sourceDocument = isConfigured ? configuredDocument : options.AsyncApi;
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
            ApplyInfoDefaults(clone, options, documentName);

            var generatedItems = GenerateChannelsFromMethods(clone.Components, options, asyncApiTypes)
                .Concat(GenerateChannelsFromClasses(clone.Components, options, asyncApiTypes))
                .Concat(GenerateChannelsFromDiscoveredConsumers(clone.Components, options, documentName));
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
            foreach (var filterDescriptor in options.DocumentFilters)
            {
                var filter = ResolveFilter<IDocumentFilter>(filterDescriptor);
                filter.Apply(clone, filterContext);
            }

            if (clone.Channels.Count == 0 && clone.Operations.Count == 0)
            {
                var scanAssemblies = GetScanAssemblies(options, documentName);
                _logger.LogWarning(
                    "AsyncAPI document '{DocumentName}' has no channels or operations. " +
                    "Scanned assemblies: {ScannedAssemblies}. Types are included when marked [AsyncApi] " +
                    "with a document name matching '{AttributeDocumentName}'. If you rely on entry-assembly " +
                    "scanning, note that test hosts report the test runner as the entry assembly; " +
                    "set AsyncApiOptions.AssemblyMarkerTypes explicitly in that case.",
                    documentName ?? "<default>",
                    scanAssemblies.Count == 0 ? "<none>" : string.Join(", ", scanAssemblies.Select(a => a.GetName().Name)),
                    GetAttributeDocumentName(options, documentName) ?? "<none>");
            }

            _documentValidator.Validate(clone);
            return clone;
        }

        private static IReadOnlyList<Assembly> GetScanAssemblies(AsyncApiOptions options, string? documentName)
        {
            if (documentName is not null
                && options.Documents.TryGetValue(documentName, out var registration)
                && registration.MarkerTypes.Count > 0)
            {
                return registration.MarkerTypes.Select(t => t.Assembly).Distinct().ToArray();
            }

            return options.GetEffectiveScanAssemblies();
        }

        private static string? GetAttributeDocumentName(AsyncApiOptions options, string? documentName)
        {
            return documentName is not null && options.Documents.TryGetValue(documentName, out var registration)
                ? registration.AttributeDocumentName
                : documentName;
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
                foreach (var generated in GenerateForMember(components, options, item.Method, item.Channel!, GetOperationAttributes(item.Method).ToArray()))
                {
                    yield return generated;
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
                foreach (var generated in GenerateForMember(components, options, item.Type, item.Channel!, GetOperationAttributes(item.Type).ToArray()))
                {
                    yield return generated;
                }
            }
        }

        /// <summary>
        /// Documents MassTransit consumers found by convention when
        /// <see cref="AsyncApiDiscoveryOptions.DiscoverMassTransitConsumers"/> is enabled, feeding the
        /// synthesized channel/operation attributes through the same pipeline as reflected attributes.
        /// </summary>
        private IEnumerable<GeneratedOperation> GenerateChannelsFromDiscoveredConsumers(
            AsyncApiComponentsDescriptor components,
            AsyncApiOptions options,
            string? documentName)
        {
            if (!options.Discovery.DiscoverMassTransitConsumers)
            {
                yield break;
            }

            var sourceTypes = GetScanScopeTypes(options, documentName);
            foreach (var discovered in MassTransitConsumerDiscovery.Discover(sourceTypes, options.Discovery))
            {
                foreach (var generated in GenerateForMember(components, options, discovered.Method, discovered.Channel, new OperationAttribute[] { discovered.Operation }))
                {
                    yield return generated;
                }
            }
        }

        /// <summary>
        /// Resolves the full set of types in a document's scan scope (unlike
        /// <see cref="GetAsyncApiTypes"/>, not restricted to types carrying <c>[AsyncApi]</c>),
        /// honoring per-registration marker types and type filters.
        /// </summary>
        private static IEnumerable<TypeInfo> GetScanScopeTypes(AsyncApiOptions options, string? documentName)
        {
            var registration = documentName is not null && options.Documents.TryGetValue(documentName, out var configuredDocument)
                ? configuredDocument
                : null;
            var sourceTypes = registration is not null && registration.MarkerTypes.Count > 0
                ? registration.MarkerTypes
                    .Select(t => t.Assembly)
                    .Distinct()
                    .SelectMany(a => a.DefinedTypes)
                : options.AsyncApiSchemaTypes;

            return registration?.TypeFilter is { } typeFilter
                ? sourceTypes.Where(typeFilter)
                : sourceTypes;
        }

        /// <summary>
        /// Runs the full channel/operation generation pipeline (message resolution, channel and
        /// operation building, filters, reply channels) for one annotated member. The channel and
        /// operation attributes are passed in so callers can supply either reflected or
        /// convention-synthesized attributes.
        /// </summary>
        private IEnumerable<GeneratedOperation> GenerateForMember(
            AsyncApiComponentsDescriptor components,
            AsyncApiOptions options,
            MemberInfo member,
            ChannelAttribute channel,
            OperationAttribute[] operationAttributes)
        {
            var replyMessageOperation = GetReplyMessageOperation(member, operationAttributes);
            foreach (var operationAttribute in operationAttributes)
            {
                ValidateReplyConfiguration(member, operationAttribute);
            }

            var operationMessages = operationAttributes
                .ToDictionary(
                    operationAttribute => operationAttribute,
                    operationAttribute => member switch
                    {
                        MethodInfo method => _messageResolver.ResolveForOperation(method, operationAttribute, options.Inference),
                        TypeInfo type => _messageResolver.ResolveForOperation(type, operationAttribute, options.Inference),
                        _ => throw new ArgumentException($"Unsupported member kind '{member.GetType().Name}'.", nameof(member)),
                    });
            var replyMessages = operationAttributes
                .ToDictionary(
                    operationAttribute => operationAttribute,
                    operationAttribute => replyMessageOperation is not null && !ReferenceEquals(replyMessageOperation, operationAttribute)
                        ? EmptyMessageResolution()
                        : member switch
                        {
                            MethodInfo method => _messageResolver.ResolveReplyForOperation(method, operationAttribute, options.Inference),
                            TypeInfo type => _messageResolver.ResolveReplyForOperation(type, operationAttribute, options.Inference),
                            _ => throw new ArgumentException($"Unsupported member kind '{member.GetType().Name}'.", nameof(member)),
                        });

            RegisterMessageResolutions(components, operationMessages.Values.Concat(replyMessages.Values));
            var channelItem = _channelBuilder.Build(member, channel, UnionMessageIds(operationMessages.Values), options.Inference);
            RegisterChannelParameters(components, channelItem);

            ApplyChannelFilters(options, member, channel, channelItem);

            foreach (var pair in operationMessages)
            {
                var replyResolution = replyMessages[pair.Key];
                var replyMessageIds = GetReplyMessageIds(pair.Key, pair.Value, replyResolution);
                var operation = _operationBuilder.Build(member, pair.Key, channelItem.Id, pair.Value.MessageIds, replyMessageIds);
                ApplyOperationFilters(member, options, pair.Key, operation);

                yield return new GeneratedOperation(
                    channelItem.Id,
                    channelItem,
                    GetOperationId(pair.Key, member, pair.Key.Action, options),
                    operation,
                    member);

                if (TryCreateReplyChannel(channelItem, pair.Key, operation, out var replyChannel))
                {
                    ApplyChannelFilters(options, member, replyChannel);

                    yield return new GeneratedOperation(
                        replyChannel.Id,
                        replyChannel,
                        null,
                        null,
                        member);
                }
            }
        }

        private void ApplyChannelFilters(AsyncApiOptions options, MemberInfo member, ChannelAttribute channel, AsyncApiChannelDescriptor channelItem)
        {
            var context = new ChannelFilterContext(member, channel);

            foreach (var filterDescriptor in options.ChannelFilters)
            {
                var filter = ResolveFilter<IChannelFilter>(filterDescriptor);
                filter.Apply(channelItem, context);
            }
        }

        private void ApplyChannelFilters(AsyncApiOptions options, MemberInfo member, AsyncApiChannelDescriptor channelItem)
        {
            ApplyChannelFilters(options, member, CreateChannelFilterAttribute(channelItem), channelItem);
        }

        private void ApplyOperationFilters(MemberInfo member, AsyncApiOptions options, OperationAttribute operationAttribute, AsyncApiOperationDescriptor operation)
        {
            var filterContext = new OperationFilterContext(member, operationAttribute);

            foreach (var filterDescriptor in options.OperationFilters)
            {
                var filter = ResolveFilter<IOperationFilter>(filterDescriptor);
                filter.Apply(operation, filterContext);
            }
        }

        private TFilter ResolveFilter<TFilter>(FilterDescriptor descriptor)
            where TFilter : class
        {
            if (descriptor.FilterInstance is TFilter instance)
            {
                return instance;
            }

            return _serviceProvider.GetService(descriptor.FilterType!) as TFilter
                ?? (TFilter)ActivatorUtilities.CreateInstance(_serviceProvider, descriptor.FilterType!);
        }

        private void RegisterMessageResolutions(AsyncApiComponentsDescriptor components, IEnumerable<AsyncApiMessageResolutionDescriptor> resolutions)
        {
            foreach (var resolution in resolutions)
            {
                foreach (var schema in resolution.Schemas)
                {
                    if (components.Schemas.TryGetValue(schema.Id, out var existingSchema))
                    {
                        if (!AttributeMessageResolver.SchemaDescriptorsMatch(existingSchema, schema.Schema))
                        {
                            throw new InvalidOperationException(
                                $"Conflicting schema definitions were generated for component id '{schema.Id}'. " +
                                $"Existing definition: {AttributeMessageResolver.FormatSchemaDescriptor(existingSchema)}. " +
                                $"Incoming definition: {AttributeMessageResolver.FormatSchemaDescriptor(schema.Schema)}. " +
                                "This usually means two payload types share the same simple name; give one an explicit schema id or move it to avoid the collision.");
                        }

                        continue;
                    }

                    components.Schemas[schema.Id] = schema.Schema;
                }

                foreach (var message in resolution.Messages)
                {
                    if (components.Messages.TryGetValue(message.Id, out var existingMessage))
                    {
                        if (!AttributeMessageResolver.MessageDescriptorsMatch(existingMessage, message))
                        {
                            throw new InvalidOperationException(
                                $"Conflicting message definitions were generated for component id '{message.Id}'. " +
                                $"Existing definition: {AttributeMessageResolver.FormatMessageDescriptor(existingMessage)}. " +
                                $"Incoming definition: {AttributeMessageResolver.FormatMessageDescriptor(message)}. " +
                                "This usually means two payload types share the same simple name; give one an explicit message id or move it to avoid the collision.");
                        }

                        continue;
                    }

                    components.Messages[message.Id] = message;
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

        private static AsyncApiMessageResolutionDescriptor EmptyMessageResolution()
        {
            return new AsyncApiMessageResolutionDescriptor(
                Array.Empty<string>(),
                Array.Empty<AsyncApiMessageDescriptor>(),
                Array.Empty<AsyncApiSchemaComponentDescriptor>());
        }

        private static OperationAttribute? GetReplyMessageOperation(
            MemberInfo member,
            IReadOnlyCollection<OperationAttribute> operationAttributes)
        {
            if (!HasReplyMessageAttributes(member))
            {
                return null;
            }

            var replyOperations = operationAttributes
                .Where(operationAttribute => !string.IsNullOrWhiteSpace(operationAttribute.Reply))
                .ToArray();
            if (replyOperations.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Operation member '{FormatMember(member)}' has [ReplyMessage] annotations but no Reply channel id. Set Reply on the surrounding operation or remove the reply messages.");
            }

            if (replyOperations.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Operation member '{FormatMember(member)}' has [ReplyMessage] annotations and multiple operations with Reply configured. " +
                    "Move each operation to a separate member or use the operation-specific ReplyMessagePayloadType properties so every reply message has an unambiguous owner.");
            }

            return replyOperations[0];
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
                if (operationAttribute.ReplyMessagePayloadType is null
                    && HasLegacyReplyMessageMetadata(operationAttribute))
                {
                    throw new InvalidOperationException(
                        $"Operation '{FormatMember(member)}' configures reply message metadata but no ReplyMessagePayloadType. Set ReplyMessagePayloadType or move the metadata to a [ReplyMessage] attribute.");
                }

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

            if (HasLegacyReplyMessageMetadata(operationAttribute))
            {
                throw new InvalidOperationException(
                    $"Operation '{FormatMember(member)}' configures reply message metadata but no Reply channel id. Set OperationAttribute.Reply and ReplyMessagePayloadType, or remove the reply metadata.");
            }

        }

        private static bool HasLegacyReplyMessageMetadata(OperationAttribute operationAttribute)
        {
            return !string.IsNullOrWhiteSpace(operationAttribute.ReplyMessagePayloadSchemaId)
                || !string.IsNullOrWhiteSpace(operationAttribute.ReplyMessageId)
                || !string.IsNullOrWhiteSpace(operationAttribute.ReplyMessageName)
                || !string.IsNullOrWhiteSpace(operationAttribute.ReplyMessageTitle);
        }

        private static bool HasReplyMessageAttributes(MemberInfo member)
        {
            if (member.GetCustomAttributes<ReplyMessageAttribute>().Any())
            {
                return true;
            }

            return member is TypeInfo type
                && type.DeclaredMethods
                    .Where(method => !GetOperationAttributes(method).Any())
                    .Any(method => method.GetCustomAttributes<ReplyMessageAttribute>().Any());
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
            var hasDynamicReplyAddress = !string.IsNullOrWhiteSpace(operation.Reply.AddressLocation);
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
                hasDynamicReplyAddress ? null : sourceChannel.BindingsRef,
                sourceChannel.ServerNames,
                replyMessageIds.ToArray(),
                Array.Empty<AsyncApiParameterDescriptor>());
            if (!hasDynamicReplyAddress)
            {
                replyChannel = replyChannel with { Bindings = sourceChannel.InlineBindings };
            }

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

        private static ChannelAttribute CreateChannelFilterAttribute(AsyncApiChannelDescriptor channel)
        {
            var attribute = channel.Address is null
                ? new ChannelAttribute()
                : new ChannelAttribute(channel.Id, channel.Address);

            attribute.ChannelId = channel.Id;
            attribute.Title = channel.Title;
            attribute.Summary = channel.Summary;
            attribute.Description = channel.Description;
            attribute.BindingsRef = channel.BindingsRef;
            attribute.Tags = channel.Tags
                .Select(tag => tag.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .ToArray();
            attribute.Servers = channel.ServerNames.ToArray();
            return attribute;
        }

        /// <summary>
        /// Fills in <c>info.title</c> and <c>info.version</c> from the document's first effective scan
        /// assembly when the user has not configured them, so generated documents always carry the
        /// spec-required info fields.
        /// </summary>
        private static void ApplyInfoDefaults(AsyncApiDocumentDescriptor document, AsyncApiOptions options, string? documentName)
        {
            document.Info ??= new AsyncApiInfoDescriptor();
            if (document.Info.Title is not null && document.Info.Version is not null)
            {
                return;
            }

            var sourceAssembly = GetFirstScanAssembly(options, documentName);
            document.Info.Title ??= sourceAssembly?.GetName().Name ?? "AsyncAPI Document";
            document.Info.Version ??= GetAssemblyVersion(sourceAssembly) ?? "1.0.0";
        }

        private static Assembly? GetFirstScanAssembly(AsyncApiOptions options, string? documentName)
        {
            if (documentName is not null
                && options.Documents.TryGetValue(documentName, out var registration)
                && registration.MarkerTypes.Count > 0)
            {
                return registration.MarkerTypes[0].Assembly;
            }

            return options.GetEffectiveScanAssemblies().FirstOrDefault();
        }

        private static string? GetAssemblyVersion(Assembly? assembly)
        {
            if (assembly is null)
            {
                return null;
            }

            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var metadataSeparator = informationalVersion.IndexOf('+');
                return metadataSeparator >= 0 ? informationalVersion.Substring(0, metadataSeparator) : informationalVersion;
            }

            return assembly.GetName().Version?.ToString();
        }

        private static TypeInfo[] GetAsyncApiTypes(AsyncApiOptions options, string? apiName)
        {
            var registration = apiName is not null && options.Documents.TryGetValue(apiName, out var configuredDocument)
                ? configuredDocument
                : null;
            var markerTypes = registration is not null && registration.MarkerTypes.Count > 0
                ? registration.MarkerTypes
                : null;
            var sourceTypes = markerTypes
                is null
                ? options.AsyncApiSchemaTypes
                : markerTypes
                    .Select(t => t.Assembly)
                    .Distinct()
                    .SelectMany(a => a.DefinedTypes)
                    .ToImmutableHashSet();
            var attributeDocumentName = registration?.AttributeDocumentName ?? apiName;

            return sourceTypes
                .Where(t => t.GetCustomAttribute<AsyncApiAttribute>()?.DocumentName == attributeDocumentName)
                .Where(t => registration?.TypeFilter?.Invoke(t) ?? true)
                .ToArray();
        }

        private static bool TryGetConfiguredDocument(AsyncApiOptions options, string? documentName, out AsyncApiDocumentDescriptor document)
        {
            if (documentName is not null && options.Documents.TryGetValue(documentName, out var configuredDocument))
            {
                document = configuredDocument.Document;
                return true;
            }

            if (documentName is not null && options.NamedApis.TryGetValue(documentName, out var namedDocument))
            {
                document = namedDocument;
                return true;
            }

            document = default!;
            return false;
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
