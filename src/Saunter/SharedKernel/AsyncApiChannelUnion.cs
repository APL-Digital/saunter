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
            return source
                .Concat(additional)
                .DistinctBy(parameter => parameter.Name)
                .ToList();
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
