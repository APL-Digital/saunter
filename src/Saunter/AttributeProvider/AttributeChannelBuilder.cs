using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options;

namespace Saunter.AttributeProvider
{
    internal class AttributeChannelBuilder : IAttributeChannelBuilder
    {
        private static readonly Regex s_channelParameterNamePattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);
        private static readonly Regex s_channelAddressExpressionPattern = new(@"\{([A-Za-z0-9_-]+)\}", RegexOptions.Compiled);

        public AsyncApiChannelDescriptor Build(MemberInfo member, ChannelAttribute attribute, IReadOnlyList<string> messageIds, AsyncApiInferenceOptions inferenceOptions)
        {
            var address = ResolveAddress(member, attribute, inferenceOptions);
            var channelId = ResolveChannelId(attribute, address, inferenceOptions);
            var channel = new AsyncApiChannelDescriptor(
                channelId,
                address,
                attribute.Title,
                attribute.Summary,
                attribute.Description,
                attribute.BindingsRef,
                attribute.Servers,
                messageIds.ToArray(),
                BuildChannelParameters(member, address));

            var tagsByName = new Dictionary<string, ByteBard.AsyncAPI.Models.AsyncApiTag>(StringComparer.Ordinal);
            foreach (var tag in attribute.Tags)
            {
                tagsByName[tag] = new ByteBard.AsyncAPI.Models.AsyncApiTag { Name = tag };
            }

            foreach (var tagAttribute in member.GetCustomAttributes<ChannelTagAttribute>())
            {
                tagsByName[tagAttribute.Name] = new ByteBard.AsyncAPI.Models.AsyncApiTag
                {
                    Name = tagAttribute.Name,
                    Description = tagAttribute.Description,
                    ExternalDocs = AttributeProviderModelFactory.CreateExternalDocs(tagAttribute.ExternalDocs, tagAttribute.ExternalDocsDescription),
                };
            }

            foreach (var tag in tagsByName.Values)
            {
                channel.Tags.Add(tag);
            }

            return channel;
        }

        private static IReadOnlyList<AsyncApiParameterDescriptor> BuildChannelParameters(MemberInfo member, string address)
        {
            var expressionNames = GetChannelAddressParameterNames(address);
            var attributes = member.GetCustomAttributes<ChannelParameterAttribute>().ToArray();
            var parameters = new Dictionary<string, AsyncApiParameterDescriptor>(attributes.Length);

            foreach (var attribute in attributes)
            {
                if (!s_channelParameterNamePattern.IsMatch(attribute.Name))
                {
                    throw new InvalidOperationException($"Channel parameter '{attribute.Name}' is not a valid AsyncAPI parameter name. Use only letters, digits, '-', or '_'.");
                }

                parameters.Add(attribute.Name, CreateChannelParameter(attribute));
            }

            foreach (var parameterName in parameters.Keys.Except(expressionNames))
            {
                throw new InvalidOperationException($"Channel parameter '{parameterName}' is not present in address '{address}'. Remove [ChannelParameter(\"{parameterName}\")] or add '{{{parameterName}}}' to the address.");
            }

            foreach (var parameterName in expressionNames)
            {
                if (!parameters.ContainsKey(parameterName))
                {
                    parameters[parameterName] = CreateChannelParameter(parameterName);
                }
            }

            return parameters.Values.ToArray();
        }

        private static IReadOnlyCollection<string> GetChannelAddressParameterNames(string address)
        {
            if (address.Contains('?') || address.Contains('#'))
            {
                throw new InvalidOperationException($"Channel address '{address}' must not contain query strings or fragments. Move that data into parameters or message content.");
            }

            var matches = s_channelAddressExpressionPattern.Matches(address);
            var strippedAddress = s_channelAddressExpressionPattern.Replace(address, string.Empty);
            if (strippedAddress.Contains('{') || strippedAddress.Contains('}'))
            {
                throw new InvalidOperationException($"Channel address '{address}' contains an invalid expression. Check that each parameter placeholder is balanced like '{{parameterName}}'.");
            }

            return matches
                .Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static AsyncApiParameterDescriptor CreateChannelParameter(ChannelParameterAttribute attribute)
        {
            var enumValues = attribute.Type.IsEnum
                ? Enum.GetNames(attribute.Type)
                : Array.Empty<string>();

            return new AsyncApiParameterDescriptor(
                attribute.Name,
                attribute.Description,
                attribute.Location,
                enumValues,
                attribute.DefaultValue,
                attribute.Examples ?? Array.Empty<string>());
        }

        private static AsyncApiParameterDescriptor CreateChannelParameter(string name)
        {
            return new AsyncApiParameterDescriptor(name, null, null, Array.Empty<string>(), null, Array.Empty<string>());
        }

        private static string ResolveAddress(MemberInfo member, ChannelAttribute attribute, AsyncApiInferenceOptions inferenceOptions)
        {
            if (!string.IsNullOrWhiteSpace(attribute.Address))
            {
                return attribute.Address;
            }

            if (inferenceOptions.InferChannelAddressFromRoute && TryResolveRouteTemplate(member, out var routeTemplate))
            {
                return routeTemplate;
            }

            throw new InvalidOperationException($"Channel address is missing for '{member.Name}'. Set [Channel(\"address\")] explicitly or enable route-based inference on a member with route metadata.");
        }

        private static string ResolveChannelId(ChannelAttribute attribute, string address, AsyncApiInferenceOptions inferenceOptions)
        {
            if (!string.IsNullOrWhiteSpace(attribute.ChannelId))
            {
                return attribute.ChannelId;
            }

            if (inferenceOptions.InferChannelIdFromAddress)
            {
                return AttributeProviderModelFactory.SanitizeComponentKey(inferenceOptions.ChannelIdGenerator(address));
            }

            throw new InvalidOperationException($"Channel id is missing for address '{address}'. Set [Channel(\"channelId\", \"{address}\")] explicitly or enable channel id inference.");
        }

        private static bool TryResolveRouteTemplate(MemberInfo member, out string template)
        {
            var routeAttribute = member
                .GetCustomAttributes()
                .FirstOrDefault(attribute => attribute.GetType().GetProperty("Template")?.PropertyType == typeof(string));
            template = routeAttribute?.GetType().GetProperty("Template")?.GetValue(routeAttribute) as string ?? string.Empty;
            return !string.IsNullOrWhiteSpace(template);
        }
    }
}
