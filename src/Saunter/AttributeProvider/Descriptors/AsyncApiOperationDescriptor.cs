using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;

namespace Saunter.AttributeProvider.Descriptors
{
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

        public IList<string> TraitReferences { get; } = new List<string>();

        public AsyncApiBindings<IOperationBinding> InlineBindings => _bindings;

        public AsyncApiBindings<IOperationBinding> Bindings
        {
            get => global::Saunter.AttributeProvider.AttributeProviderModelFactory.ResolveBindings<IOperationBinding>(BindingsRef, _bindings, "operationBindings");
            init => _bindings = value ?? new AsyncApiBindings<IOperationBinding>();
        }
    }
}
