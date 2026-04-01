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
                throw new InvalidOperationException("Channel address conflict");
            }

            if (!string.IsNullOrWhiteSpace(source.BindingsRef)
                && !string.IsNullOrWhiteSpace(additional.BindingsRef)
                && !string.Equals(source.BindingsRef, additional.BindingsRef, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Channel '{source.Id}' has conflicting bindings references '{source.BindingsRef}' and '{additional.BindingsRef}'.");
            }

            var merged = new AsyncApiChannelDescriptor(
                source.Id,
                FirstNonBlank(source.Address, additional.Address) ?? source.Address,
                source.Title ?? additional.Title,
                source.Summary ?? additional.Summary,
                source.Description ?? additional.Description,
                source.BindingsRef ?? additional.BindingsRef,
                MergeStrings(source.ServerNames, additional.ServerNames),
                MergeStrings(source.MessageIds, additional.MessageIds),
                MergeParameters(source.Parameters, additional.Parameters));

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

        private static IReadOnlyList<AsyncApiParameterDescriptor> MergeParameters(IReadOnlyList<AsyncApiParameterDescriptor> source, IReadOnlyList<AsyncApiParameterDescriptor> additional)
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
                    throw new InvalidOperationException($"Channel parameter '{parameter.Name}' has conflicting definitions.");
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
    }
}
