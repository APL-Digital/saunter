using System;
using System.Collections.Generic;
using System.Linq;
using Saunter.AttributeProvider.Descriptors;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiChannelUnion : IAsyncApiChannelUnion
    {
        public AsyncApiChannelDescriptor Union(AsyncApiChannelDescriptor source, AsyncApiChannelDescriptor additional)
        {
            if (!string.IsNullOrWhiteSpace(source.Address)
                && !string.IsNullOrWhiteSpace(additional.Address)
                && !string.Equals(source.Address, additional.Address, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Channel '{source.Id}' has conflicting addresses '{source.Address}' and '{additional.Address}'. " +
                    $"Existing definition: {FormatChannel(source)}. Incoming definition: {FormatChannel(additional)}.");
            }

            if (!string.IsNullOrWhiteSpace(source.BindingsRef)
                && !string.IsNullOrWhiteSpace(additional.BindingsRef)
                && !string.Equals(source.BindingsRef, additional.BindingsRef, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Channel '{source.Id}' has conflicting bindings references '{source.BindingsRef}' and '{additional.BindingsRef}'. " +
                    $"Existing definition: {FormatChannel(source)}. Incoming definition: {FormatChannel(additional)}.");
            }

            var merged = new AsyncApiChannelDescriptor(
                source.Id,
                FirstNonBlank(source.Address, additional.Address) ?? source.Address,
                source.Title ?? additional.Title,
                source.Summary ?? additional.Summary,
                source.Description ?? additional.Description,
                FirstNonBlank(source.BindingsRef, additional.BindingsRef),
                MergeStrings(source.ServerNames, additional.ServerNames),
                MergeStrings(source.MessageIds, additional.MessageIds),
                MergeParameters(source.Id, source.Parameters, additional.Parameters));

            foreach (var tag in MergeTags(source.Tags, additional.Tags))
            {
                merged.Tags.Add(tag);
            }

            return merged;
        }

        private static string? FirstNonBlank(string? source, string? additional)
        {
            return !string.IsNullOrWhiteSpace(source)
                ? source
                : !string.IsNullOrWhiteSpace(additional) ? additional : null;
        }

        private static IReadOnlyList<string> MergeStrings(IReadOnlyList<string> source, IReadOnlyList<string> additional)
        {
            return source
                .Concat(additional)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static IReadOnlyList<AsyncApiParameterDescriptor> MergeParameters(string channelId, IReadOnlyList<AsyncApiParameterDescriptor> source, IReadOnlyList<AsyncApiParameterDescriptor> additional)
        {
            var parametersByName = new Dictionary<string, AsyncApiParameterDescriptor>(StringComparer.Ordinal);

            foreach (var parameter in source.Concat(additional))
            {
                if (!parametersByName.TryGetValue(parameter.Name, out var existing))
                {
                    parametersByName[parameter.Name] = parameter;
                    continue;
                }

                if (!ParametersMatch(existing, parameter))
                {
                    throw new InvalidOperationException(
                        $"Channel '{channelId}' parameter '{parameter.Name}' has conflicting definitions. " +
                        $"Existing definition: {FormatParameter(existing)}. Incoming definition: {FormatParameter(parameter)}.");
                }
            }

            return parametersByName.Values.ToList();
        }

        private static bool ParametersMatch(AsyncApiParameterDescriptor source, AsyncApiParameterDescriptor additional)
        {
            return string.Equals(source.Description, additional.Description, StringComparison.Ordinal)
                && string.Equals(source.Location, additional.Location, StringComparison.Ordinal)
                && string.Equals(source.DefaultValue, additional.DefaultValue, StringComparison.Ordinal)
                && source.EnumValues.SequenceEqual(additional.EnumValues, StringComparer.Ordinal)
                && source.Examples.SequenceEqual(additional.Examples, StringComparer.Ordinal);
        }

        private static IReadOnlyList<ByteBard.AsyncAPI.Models.AsyncApiTag> MergeTags(
            IList<ByteBard.AsyncAPI.Models.AsyncApiTag> source,
            IList<ByteBard.AsyncAPI.Models.AsyncApiTag> additional)
        {
            return source
                .Concat(additional)
                .DistinctBy(tag => tag.Name)
                .ToList();
        }

        private static string FormatChannel(AsyncApiChannelDescriptor channel)
        {
            return $"id={FormatValue(channel.Id)}, address={FormatValue(channel.Address)}, messages={FormatValues(channel.MessageIds)}, servers={FormatValues(channel.ServerNames)}";
        }

        private static string FormatParameter(AsyncApiParameterDescriptor parameter)
        {
            return $"name={FormatValue(parameter.Name)}, description={FormatValue(parameter.Description)}, location={FormatValue(parameter.Location)}, default={FormatValue(parameter.DefaultValue)}, enum={FormatValues(parameter.EnumValues)}, examples={FormatValues(parameter.Examples)}";
        }

        private static string FormatValues(IEnumerable<string> values)
        {
            var materialized = values.ToArray();
            return materialized.Length == 0
                ? "[]"
                : $"[{string.Join(", ", materialized.Select(FormatValue))}]";
        }

        private static string FormatValue(string? value)
        {
            return value is null ? "<null>" : $"'{value}'";
        }
    }
}
