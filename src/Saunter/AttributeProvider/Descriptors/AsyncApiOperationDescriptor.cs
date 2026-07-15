using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;

namespace Saunter.AttributeProvider.Descriptors
{
    /// <summary>
    /// Describes an AsyncAPI <c>operation</c> object generated from operation attributes.
    /// </summary>
    /// <param name="Action">The AsyncAPI <c>operation.action</c> field: whether the application sends or receives messages.</param>
    /// <param name="ChannelId">The id of the channel (key in the document's <c>channels</c> map) the operation is performed on.</param>
    /// <param name="Title">The AsyncAPI <c>operation.title</c> field: a human-friendly title for the operation.</param>
    /// <param name="Summary">The AsyncAPI <c>operation.summary</c> field: a short summary of the operation.</param>
    /// <param name="Description">The AsyncAPI <c>operation.description</c> field: an optional description of the operation. CommonMark syntax can be used.</param>
    /// <param name="BindingsRef">The name of an operation bindings item in components/operationBindings to resolve <see cref="Bindings"/> from, or <c>null</c> to use the inline bindings.</param>
    /// <param name="MessageIds">The ids of the messages (keys in components/messages) the operation sends or receives.</param>
    /// <param name="Tags">The AsyncAPI <c>operation.tags</c> field: tag names for logical grouping of operations.</param>
    /// <param name="Reply">The AsyncAPI <c>operation.reply</c> object for request/reply operations, or <c>null</c>.</param>
    public sealed record AsyncApiOperationDescriptor(
        AsyncApiAction Action,
        string ChannelId,
        string? Title,
        string? Summary,
        string? Description,
        string? BindingsRef,
        IReadOnlyList<string> MessageIds,
        IReadOnlyList<string> Tags,
        AsyncApiOperationReplyDescriptor? Reply)
    {
        private AsyncApiBindings<IOperationBinding> _bindings = new();

        /// <summary>
        /// The names of operation traits in components/operationTraits applied to this operation
        /// (AsyncAPI <c>operation.traits</c>).
        /// </summary>
        public IList<string> TraitReferences { get; } = new List<string>();

        /// <summary>
        /// The operation bindings declared directly on this descriptor, ignoring <see cref="BindingsRef"/>.
        /// </summary>
        public AsyncApiBindings<IOperationBinding> InlineBindings => _bindings;

        /// <summary>
        /// The effective operation bindings: resolved from components/operationBindings when
        /// <see cref="BindingsRef"/> is set, otherwise the inline bindings.
        /// </summary>
        public AsyncApiBindings<IOperationBinding> Bindings
        {
            get => global::Saunter.AttributeProvider.AttributeProviderModelFactory.ResolveBindings<IOperationBinding>(BindingsRef, _bindings, "operationBindings");
            init => _bindings = value ?? new AsyncApiBindings<IOperationBinding>();
        }
    }
}
