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
        public IList<string> TraitReferences { get; } = new List<string>();

        public AsyncApiBindings<IOperationBinding> Bindings => AttributeProvider.AttributeProviderModelFactory.CreateBindingsReference<IOperationBinding>(BindingsRef, "operationBindings");
    }
}
