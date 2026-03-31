using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;

namespace Saunter.AttributeProvider
{
    internal class AttributeChannelBuilder : IAttributeChannelBuilder
    {
        private static readonly Regex s_channelParameterNamePattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);
        private static readonly Regex s_channelAddressExpressionPattern = new(@"\{([A-Za-z0-9_-]+)\}", RegexOptions.Compiled);

        public AsyncApiChannelDescriptor Build(MemberInfo member, ChannelAttribute attribute, IReadOnlyList<string> messageIds)
        {
            return new AsyncApiChannelDescriptor(
                attribute.ChannelId,
                attribute.Address,
                attribute.Title,
                attribute.Summary,
                attribute.Description,
                attribute.BindingsRef,
                attribute.Servers,
                messageIds.ToArray(),
                BuildChannelParameters(member, attribute.Address));
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
                    throw new InvalidOperationException($"Channel parameter '{attribute.Name}' is not a valid AsyncAPI parameter name.");
                }

                parameters.Add(attribute.Name, CreateChannelParameter(attribute));
            }

            foreach (var parameterName in parameters.Keys.Except(expressionNames))
            {
                throw new InvalidOperationException($"Channel parameter '{parameterName}' is not present in address '{address}'.");
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
                throw new InvalidOperationException($"Channel address '{address}' must not contain query strings or fragments.");
            }

            var matches = s_channelAddressExpressionPattern.Matches(address);
            var strippedAddress = s_channelAddressExpressionPattern.Replace(address, string.Empty);
            if (strippedAddress.Contains('{') || strippedAddress.Contains('}'))
            {
                throw new InvalidOperationException($"Channel address '{address}' contains an invalid expression.");
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
                enumValues);
        }

        private static AsyncApiParameterDescriptor CreateChannelParameter(string name)
        {
            return new AsyncApiParameterDescriptor(name, null, null, Array.Empty<string>());
        }
    }
}
