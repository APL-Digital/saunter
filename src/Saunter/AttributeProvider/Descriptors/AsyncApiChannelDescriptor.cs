using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;

namespace Saunter.AttributeProvider.Descriptors
{
    public sealed record AsyncApiChannelDescriptor(
        string Id,
        string? Address,
        string? Title,
        string? Summary,
        string? Description,
        string? BindingsRef,
        IReadOnlyList<string> ServerNames,
        IReadOnlyList<string> MessageIds,
        IReadOnlyList<AsyncApiParameterDescriptor> Parameters)
    {
        private AsyncApiBindings<IChannelBinding> _bindings = new();

        public IList<AsyncApiTag> Tags { get; } = new List<AsyncApiTag>();

        public AsyncApiBindings<IChannelBinding> InlineBindings => _bindings;

        public AsyncApiBindings<IChannelBinding> Bindings
        {
            get => global::Saunter.AttributeProvider.AttributeProviderModelFactory.ResolveBindings<IChannelBinding>(BindingsRef, _bindings, "channelBindings");
            init => _bindings = value ?? new AsyncApiBindings<IChannelBinding>();
        }

        public IReadOnlyDictionary<string, string> Messages => MessageIds.ToDictionary(id => id, id => id);

        public IReadOnlyList<string> Servers => ServerNames;
    }
}
