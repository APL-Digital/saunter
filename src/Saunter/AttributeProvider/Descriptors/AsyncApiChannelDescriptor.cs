using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;

namespace Saunter.AttributeProvider.Descriptors
{
    /// <summary>
    /// Describes an AsyncAPI <c>channel</c> object generated from channel attributes.
    /// </summary>
    /// <param name="Id">The channel key in the document's <c>channels</c> map.</param>
    /// <param name="Address">The AsyncAPI <c>channel.address</c> field: the topic, queue or route the channel is bound to.</param>
    /// <param name="Title">The AsyncAPI <c>channel.title</c> field: a human-friendly title for the channel.</param>
    /// <param name="Summary">The AsyncAPI <c>channel.summary</c> field: a short summary of the channel.</param>
    /// <param name="Description">The AsyncAPI <c>channel.description</c> field: an optional description of the channel. CommonMark syntax can be used.</param>
    /// <param name="BindingsRef">The name of a channel bindings item in components/channelBindings to resolve <see cref="Bindings"/> from, or <c>null</c> to use the inline bindings.</param>
    /// <param name="ServerNames">The names of the servers (keys in the document's <c>servers</c> map) the channel is available on. Empty means all servers.</param>
    /// <param name="MessageIds">The ids of the messages (keys in components/messages) that can flow through the channel.</param>
    /// <param name="Parameters">The channel parameters that appear as <c>{parameter}</c> segments in the address.</param>
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

        /// <summary>
        /// The AsyncAPI <c>channel.tags</c> field: a list of tags for logical grouping of channels.
        /// </summary>
        public IList<AsyncApiTag> Tags { get; } = new List<AsyncApiTag>();

        /// <summary>
        /// The channel bindings declared directly on this descriptor, ignoring <see cref="BindingsRef"/>.
        /// </summary>
        public AsyncApiBindings<IChannelBinding> InlineBindings => _bindings;

        /// <summary>
        /// The effective channel bindings: resolved from components/channelBindings when
        /// <see cref="BindingsRef"/> is set, otherwise the inline bindings.
        /// </summary>
        public AsyncApiBindings<IChannelBinding> Bindings
        {
            get => global::Saunter.AttributeProvider.AttributeProviderModelFactory.ResolveBindings<IChannelBinding>(BindingsRef, _bindings, "channelBindings");
            init => _bindings = value ?? new AsyncApiBindings<IChannelBinding>();
        }

        /// <summary>
        /// The AsyncAPI <c>channel.messages</c> map derived from <see cref="MessageIds"/>: each entry
        /// references the message of the same id in components/messages.
        /// </summary>
        public IReadOnlyDictionary<string, string> Messages => MessageIds.ToDictionary(id => id, id => id);

        /// <summary>
        /// The AsyncAPI <c>channel.servers</c> field: the server names the channel is available on.
        /// Alias for <see cref="ServerNames"/>.
        /// </summary>
        public IReadOnlyList<string> Servers => ServerNames;
    }
}
