using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;

namespace Saunter.AttributeProvider.Descriptors
{
    public sealed record AsyncApiMessageDescriptor(
        string Id,
        string Name,
        string Title,
        string? Summary,
        string? Description,
        string? PayloadSchemaId,
        string? HeadersSchemaId,
        string? CorrelationIdRef,
        string? ContentType,
        string? ExternalDocsUrl,
        string? ExternalDocsDescription,
        string? BindingsRef,
        IReadOnlyList<string> Tags)
    {
        private AsyncApiBindings<IMessageBinding> _bindings = new();

        public AsyncApiBindings<IMessageBinding> InlineBindings => _bindings;

        public AsyncApiBindings<IMessageBinding> Bindings
        {
            get => global::Saunter.AttributeProvider.AttributeProviderModelFactory.ResolveBindings<IMessageBinding>(BindingsRef, _bindings, "messageBindings");
            init => _bindings = value ?? new AsyncApiBindings<IMessageBinding>();
        }
    }
}
