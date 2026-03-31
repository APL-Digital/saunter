using System;
using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiChannelUnion : IAsyncApiChannelUnion
    {
        public AsyncApiChannel Union(AsyncApiChannel source, AsyncApiChannel additionaly)
        {
            if (!string.IsNullOrWhiteSpace(source.Address)
                && !string.IsNullOrWhiteSpace(additionaly.Address)
                && !string.Equals(source.Address, additionaly.Address, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Channel address conflict");
            }

            var mergedMessages = new Dictionary<string, AsyncApiMessage>();
            foreach (var pair in source.Messages)
            {
                mergedMessages[pair.Key] = pair.Value;
            }

            foreach (var pair in additionaly.Messages)
            {
                mergedMessages[pair.Key] = pair.Value;
            }

            var mergedParameters = new Dictionary<string, AsyncApiParameter>();
            foreach (var pair in source.Parameters)
            {
                mergedParameters[pair.Key] = pair.Value;
            }

            foreach (var pair in additionaly.Parameters)
            {
                mergedParameters[pair.Key] = pair.Value;
            }

            return new AsyncApiChannel
            {
                Address = FirstNonBlank(source.Address, additionaly.Address),
                Title = source.Title ?? additionaly.Title,
                Summary = source.Summary ?? additionaly.Summary,
                Description = source.Description ?? additionaly.Description,
                Messages = mergedMessages,
                Parameters = mergedParameters,
                Servers = MergeServers(source.Servers, additionaly.Servers),
                Tags = MergeTags(source.Tags, additionaly.Tags),
                Bindings = MergeBindings(source.Bindings, additionaly.Bindings),
                ExternalDocs = source.ExternalDocs ?? additionaly.ExternalDocs,
                Extensions = MergeExtensions(source.Extensions, additionaly.Extensions),
            };
        }

        private static string? FirstNonBlank(string? source, string? additionaly)
        {
            return !string.IsNullOrWhiteSpace(source)
                ? source
                : !string.IsNullOrWhiteSpace(additionaly) ? additionaly : null;
        }

        private static List<AsyncApiServerReference> MergeServers(IList<AsyncApiServerReference> source, IList<AsyncApiServerReference> additionaly)
        {
            return additionaly
                .Concat(source)
                .DistinctBy(server => server.Reference.Reference)
                .ToList();
        }

        private static List<AsyncApiTag> MergeTags(IList<AsyncApiTag> source, IList<AsyncApiTag> additionaly)
        {
            return additionaly
                .Concat(source)
                .DistinctBy(tag => tag.Name)
                .ToList();
        }

        private static AsyncApiBindings<IChannelBinding> MergeBindings(AsyncApiBindings<IChannelBinding> source, AsyncApiBindings<IChannelBinding> additionaly)
        {
            if (source.GetType() != typeof(AsyncApiBindings<IChannelBinding>))
            {
                return source;
            }

            if (additionaly.GetType() != typeof(AsyncApiBindings<IChannelBinding>))
            {
                return additionaly;
            }

            var mergedBindings = new AsyncApiBindings<IChannelBinding>();

            foreach (var pair in additionaly)
            {
                mergedBindings[pair.Key] = pair.Value;
            }

            foreach (var pair in source)
            {
                mergedBindings[pair.Key] = pair.Value;
            }

            return mergedBindings;
        }

        private static Dictionary<string, IAsyncApiExtension> MergeExtensions(
            IDictionary<string, IAsyncApiExtension> source,
            IDictionary<string, IAsyncApiExtension> additionaly)
        {
            var mergedExtensions = new Dictionary<string, IAsyncApiExtension>();

            foreach (var pair in additionaly)
            {
                mergedExtensions[pair.Key] = pair.Value;
            }

            foreach (var pair in source)
            {
                mergedExtensions[pair.Key] = pair.Value;
            }

            return mergedExtensions;
        }
    }
}
