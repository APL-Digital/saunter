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
            var apiNamePair = options.NamedApis.FirstOrDefault(c => c.Value.Id == documentName);
            var sourceDocument = apiNamePair.Value ?? options.AsyncApi;
            var clone = _cloner.ClonePrototype(sourceDocument);

            clone.Asyncapi = sourceDocument.Asyncapi?.StartsWith("2.") == true
                ? sourceDocument.Asyncapi
                : "3.0.0";
            clone.DefaultContentType ??= "application/json";
            clone.Components ??= new AsyncApiComponentsDescriptor();
            clone.Channels ??= new Dictionary<string, AsyncApiChannelDescriptor>();
            clone.Operations ??= new Dictionary<string, AsyncApiOperationDescriptor>();
            clone.Servers ??= new Dictionary<string, AsyncApiServerDescriptor>();

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
                var operationMessages = GetOperationAttributes(item.Method)
                    .ToDictionary(
                        operationAttribute => operationAttribute,
                        operationAttribute => _messageResolver.ResolveForOperation(item.Method, operationAttribute));

                RegisterMessageResolutions(components, operationMessages.Values);
                var channelItem = _channelBuilder.Build(item.Method, channel, UnionMessageIds(operationMessages.Values));
                RegisterChannelParameters(components, channelItem);

                ApplyChannelFilters(options, item.Method, channel, channelItem);

                foreach (var pair in operationMessages)
                {
                    var operation = _operationBuilder.Build(item.Method, pair.Key, channel.ChannelId, pair.Value.MessageIds);
                    ApplyOperationFilters(item.Method, options, pair.Key, operation);

                    yield return new GeneratedOperation(
                        channel.ChannelId,
                        channelItem,
                        GetOperationId(pair.Key, item.Method, pair.Key.Action),
                        operation);
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
                var operationMessages = GetOperationAttributes(item.Type)
                    .ToDictionary(
                        operationAttribute => operationAttribute,
                        operationAttribute => _messageResolver.ResolveForOperation(item.Type, operationAttribute));

                RegisterMessageResolutions(components, operationMessages.Values);
                var channelItem = _channelBuilder.Build(item.Type, channel, UnionMessageIds(operationMessages.Values));
                RegisterChannelParameters(components, channelItem);

                ApplyChannelFilters(options, item.Type, channel, channelItem);

                foreach (var pair in operationMessages)
                {
                    var operation = _operationBuilder.Build(item.Type, pair.Key, channel.ChannelId, pair.Value.MessageIds);
                    ApplyOperationFilters(item.Type, options, pair.Key, operation);

                    yield return new GeneratedOperation(
                        channel.ChannelId,
                        channelItem,
                        GetOperationId(pair.Key, item.Type, pair.Key.Action),
                        operation);
                }
            }
        }

        private void ApplyChannelFilters(AsyncApiOptions options, MemberInfo member, ChannelAttribute channel, AsyncApiChannelDescriptor channelItem)
        {
            var context = new ChannelFilterContext(member, channel);

            foreach (var filterType in options.ChannelFilters)
            {
                var filter = (IChannelFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(channelItem, context);
            }
        }

        private void ApplyOperationFilters(MemberInfo member, AsyncApiOptions options, OperationAttribute operationAttribute, AsyncApiOperationDescriptor operation)
        {
            var filterContext = new OperationFilterContext(member, operationAttribute);

            foreach (var filterType in options.OperationFilters)
            {
                var filter = (IOperationFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(operation, filterContext);
            }
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

        private static TypeInfo[] GetAsyncApiTypes(AsyncApiOptions options, string? apiName)
        {
            return options
                .AsyncApiSchemaTypes
                .Where(t => t.GetCustomAttribute<AsyncApiAttribute>()?.DocumentName == apiName)
                .ToArray();
        }

        private readonly record struct GeneratedOperation(string ChannelId, AsyncApiChannelDescriptor Channel, string OperationId, AsyncApiOperationDescriptor Operation);
    }
}
