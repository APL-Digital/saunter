using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;

namespace Saunter.AttributeProvider.Descriptors
{
    /// <summary>
    /// Describes an AsyncAPI <c>message</c> object generated from message attributes or inferred payload types.
    /// </summary>
    /// <param name="Id">The message key in components/messages, referenced from channels and operations.</param>
    /// <param name="Name">The AsyncAPI <c>message.name</c> field: a machine-friendly name for the message.</param>
    /// <param name="Title">The AsyncAPI <c>message.title</c> field: a human-friendly title for the message.</param>
    /// <param name="Summary">The AsyncAPI <c>message.summary</c> field: a short summary of what the message is about.</param>
    /// <param name="Description">The AsyncAPI <c>message.description</c> field: a verbose explanation of the message. CommonMark syntax can be used.</param>
    /// <param name="PayloadSchemaId">The id of the schema in components/schemas describing the message payload, or <c>null</c> when the message has no payload schema.</param>
    /// <param name="HeadersSchemaId">The id of the schema in components/schemas describing the message headers, or <c>null</c> when the message has no headers schema.</param>
    /// <param name="CorrelationIdRef">The name of a correlation id item in components/correlationIds to reference, or <c>null</c>.</param>
    /// <param name="ContentType">The AsyncAPI <c>message.contentType</c> field: the content type used to encode/decode the payload.</param>
    /// <param name="ExternalDocsUrl">An absolute URL for additional message documentation (AsyncAPI <c>message.externalDocs.url</c>).</param>
    /// <param name="ExternalDocsDescription">Optional description for the external docs link (AsyncAPI <c>message.externalDocs.description</c>).</param>
    /// <param name="BindingsRef">The name of a message bindings item in components/messageBindings to resolve <see cref="Bindings"/> from, or <c>null</c> to use the inline bindings.</param>
    /// <param name="Tags">The AsyncAPI <c>message.tags</c> field: tag names for logical grouping of messages.</param>
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

        /// <summary>
        /// The message bindings declared directly on this descriptor, ignoring <see cref="BindingsRef"/>.
        /// </summary>
        public AsyncApiBindings<IMessageBinding> InlineBindings => _bindings;

        /// <summary>
        /// The effective message bindings: resolved from components/messageBindings when
        /// <see cref="BindingsRef"/> is set, otherwise the inline bindings.
        /// </summary>
        public AsyncApiBindings<IMessageBinding> Bindings
        {
            get => global::Saunter.AttributeProvider.AttributeProviderModelFactory.ResolveBindings<IMessageBinding>(BindingsRef, _bindings, "messageBindings");
            init => _bindings = value ?? new AsyncApiBindings<IMessageBinding>();
        }
    }
}
