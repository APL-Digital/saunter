using System;
using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;
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
                Address = source.Address ?? additionaly.Address,
                Title = source.Title ?? additionaly.Title,
                Summary = source.Summary ?? additionaly.Summary,
                Description = source.Description ?? additionaly.Description,
                Messages = mergedMessages,
                Parameters = mergedParameters,
                Servers = source.Servers.Any() ? source.Servers : additionaly.Servers,
                Tags = source.Tags.Any() ? source.Tags : additionaly.Tags,
                Bindings = source.Bindings.Count > 0 ? source.Bindings : additionaly.Bindings,
                ExternalDocs = source.ExternalDocs ?? additionaly.ExternalDocs,
                Extensions = source.Extensions.Any() ? source.Extensions : additionaly.Extensions,
            };
        }
    }
}
