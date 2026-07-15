using System;
using ByteBard.AsyncAPI.Models;

namespace Saunter.AttributeProvider.Attributes
{
    /// <summary>
    /// Base class for declaring an AsyncAPI operation on the channel declared on the same member.
    /// Use <see cref="SendOperationAttribute"/> or <see cref="ReceiveOperationAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
    public abstract class OperationAttribute : Attribute
    {
        /// <summary>
        /// The AsyncAPI <c>operation.action</c> field: whether the application sends or receives messages.
        /// Set by the concrete attribute type.
        /// </summary>
        public AsyncApiAction Action { get; protected set; }

        /// <summary>
        /// The type to use to generate the message payload schema. When <c>null</c>, the payload type
        /// can be taken from <c>[Message]</c> attributes or inferred from the method signature.
        /// </summary>
        public Type? MessagePayloadType { get; protected set; }

        /// <summary>
        /// A short summary of the operation.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Used as the generated AsyncAPI v3 operation key.
        /// </summary>
        public string? OperationId { get; set; }

        /// <summary>
        /// A human-friendly title for the operation.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// An optional description of the operation.
        /// CommonMark syntax can be used for rich text representation.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The name of an operation bindings item to reference.
        /// The bindings must be added to components/operationBindings with the same name.
        /// </summary>
        public string? BindingsRef { get; set; }

        /// <summary>
        /// The id of the reply channel, making this a request/reply operation.
        /// A reply channel with this id is generated in the document. Required whenever any
        /// other <c>Reply*</c> property is set.
        /// </summary>
        public string? Reply { get; set; }

        /// <summary>
        /// The address of the generated reply channel. Mutually exclusive with
        /// <see cref="ReplyAddressLocation"/>; omit both for an address-less reply channel.
        /// </summary>
        public string? ReplyChannelAddress { get; set; }

        /// <summary>
        /// A runtime expression that locates the reply address dynamically, e.g.
        /// <c>$message.header#/replyTo</c> (AsyncAPI <c>reply.address.location</c>).
        /// Mutually exclusive with <see cref="ReplyChannelAddress"/>.
        /// </summary>
        public string? ReplyAddressLocation { get; set; }

        /// <summary>
        /// Optional description of the reply address (AsyncAPI <c>reply.address.description</c>).
        /// </summary>
        public string? ReplyAddressDescription { get; set; }

        /// <summary>
        /// The type to use to generate the reply message payload schema.
        /// Requires <see cref="Reply"/> to be set.
        /// </summary>
        public Type? ReplyMessagePayloadType { get; set; }

        /// <summary>
        /// Unique string used as the reusable AsyncAPI message key for the reply message.
        /// Defaults to a key derived from <see cref="ReplyMessagePayloadType"/>.
        /// </summary>
        public string? ReplyMessageId { get; set; }

        /// <summary>
        /// A machine-friendly name for the reply message.
        /// Defaults to a name generated from <see cref="ReplyMessagePayloadType"/>.
        /// </summary>
        public string? ReplyMessageName { get; set; }

        /// <summary>
        /// A human-friendly title for the reply message.
        /// Defaults to a title generated from <see cref="ReplyMessagePayloadType"/>.
        /// </summary>
        public string? ReplyMessageTitle { get; set; }

        /// <summary>
        /// A list of tags for API documentation control. Tags can be used for logical grouping of operations.
        /// </summary>
        public string[] Tags { get; protected set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Declares an operation with the <c>send</c> action: the application sends messages to the channel.
    /// </summary>
    public sealed class SendOperationAttribute : OperationAttribute
    {
        /// <summary>
        /// Initializes a send operation for the given payload type with tags.
        /// </summary>
        /// <param name="messagePayloadType">The type to use to generate the message payload schema.</param>
        /// <param name="tags">Tag names for logical grouping of operations.</param>
        public SendOperationAttribute(Type messagePayloadType, params string[] tags)
        {
            Action = AsyncApiAction.Send;
            MessagePayloadType = messagePayloadType;
            Tags = tags;
        }

        /// <summary>
        /// Initializes a send operation for the given payload type.
        /// </summary>
        /// <param name="messagePayloadType">The type to use to generate the message payload schema.</param>
        public SendOperationAttribute(Type messagePayloadType)
        {
            Action = AsyncApiAction.Send;
            MessagePayloadType = messagePayloadType;
        }

        /// <summary>
        /// Initializes a send operation without an explicit payload type; the payload can be taken
        /// from <c>[Message]</c> attributes or inferred from the method signature.
        /// </summary>
        public SendOperationAttribute()
        {
            Action = AsyncApiAction.Send;
        }
    }

    /// <summary>
    /// Declares an operation with the <c>receive</c> action: the application receives messages from the channel.
    /// </summary>
    public sealed class ReceiveOperationAttribute : OperationAttribute
    {
        /// <summary>
        /// Initializes a receive operation for the given payload type with tags.
        /// </summary>
        /// <param name="messagePayloadType">The type to use to generate the message payload schema.</param>
        /// <param name="tags">Tag names for logical grouping of operations.</param>
        public ReceiveOperationAttribute(Type messagePayloadType, params string[] tags)
        {
            Action = AsyncApiAction.Receive;
            MessagePayloadType = messagePayloadType;
            Tags = tags;
        }

        /// <summary>
        /// Initializes a receive operation for the given payload type.
        /// </summary>
        /// <param name="messagePayloadType">The type to use to generate the message payload schema.</param>
        public ReceiveOperationAttribute(Type messagePayloadType)
        {
            Action = AsyncApiAction.Receive;
            MessagePayloadType = messagePayloadType;
        }

        /// <summary>
        /// Initializes a receive operation without an explicit payload type; the payload can be taken
        /// from <c>[Message]</c> attributes or inferred from the method signature.
        /// </summary>
        public ReceiveOperationAttribute()
        {
            Action = AsyncApiAction.Receive;
        }
    }
}
