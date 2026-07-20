using System;

namespace Saunter.AttributeProvider.Attributes
{
    /// <summary>
    /// Declares a message that can be returned by the request/reply operation on the same member.
    /// Apply multiple attributes to describe success, business-error, and other reply variants.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class ReplyMessageAttribute : Attribute
    {
        /// <summary>
        /// Initializes a reply message for the given payload type.
        /// </summary>
        /// <param name="payloadType">The type to use to generate the reply payload schema.</param>
        public ReplyMessageAttribute(Type payloadType)
        {
            PayloadType = payloadType;
            Tags = Array.Empty<string>();
        }

        /// <summary>
        /// Initializes a reply message for the given payload type with tags.
        /// </summary>
        /// <param name="payloadType">The type to use to generate the reply payload schema.</param>
        /// <param name="tags">Tag names for logical grouping of messages.</param>
        public ReplyMessageAttribute(Type payloadType, params string[] tags)
        {
            PayloadType = payloadType;
            Tags = tags;
        }

        /// <summary>
        /// The type to use to generate the reply payload schema.
        /// </summary>
        public Type PayloadType { get; }

        /// <summary>
        /// The type to use to generate the reply message headers schema.
        /// </summary>
        public Type? HeadersType { get; set; }

        /// <summary>
        /// A machine-friendly name for the reply message.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// A human-friendly title for the reply message.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// A short summary of what the reply message is about.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// A verbose explanation of the reply message.
        /// CommonMark syntax can be used for rich text representation.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The name of a message bindings item to reference.
        /// </summary>
        public string? BindingsRef { get; set; }

        /// <summary>
        /// The name of a correlation id item to reference.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// The content type to use when encoding/decoding the reply payload.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// An absolute URL for additional reply message documentation.
        /// </summary>
        public string? ExternalDocs { get; set; }

        /// <summary>
        /// Optional description for the external docs link.
        /// </summary>
        public string? ExternalDocsDescription { get; set; }

        /// <summary>
        /// Unique string used as the reusable AsyncAPI message key for this reply.
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        /// Overrides the reply payload's schema key in components/schemas.
        /// </summary>
        public string? PayloadSchemaId { get; set; }

        /// <summary>
        /// Tags used for logical grouping of the reply message.
        /// </summary>
        public string[] Tags { get; }
    }
}
