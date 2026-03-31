using System;
using ByteBard.AsyncAPI.Models;

namespace Saunter.AttributeProvider.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
    public abstract class OperationAttribute : Attribute
    {
        public AsyncApiAction Action { get; protected set; }

        public Type? MessagePayloadType { get; protected set; }

        public string? Summary { get; set; }

        /// <summary>
        /// Used as the generated AsyncAPI v3 operation key.
        /// </summary>
        public string? OperationId { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? BindingsRef { get; set; }

        public string? Reply { get; set; }

        public string? ReplyAddressLocation { get; set; }

        public string? ReplyAddressDescription { get; set; }

        public string[] Tags { get; protected set; } = Array.Empty<string>();
    }

    public sealed class SendOperationAttribute : OperationAttribute
    {
        public SendOperationAttribute(Type messagePayloadType, params string[] tags)
        {
            Action = AsyncApiAction.Send;
            MessagePayloadType = messagePayloadType;
            Tags = tags;
        }

        public SendOperationAttribute(Type messagePayloadType)
        {
            Action = AsyncApiAction.Send;
            MessagePayloadType = messagePayloadType;
        }

        public SendOperationAttribute()
        {
            Action = AsyncApiAction.Send;
        }
    }

    public sealed class ReceiveOperationAttribute : OperationAttribute
    {
        public ReceiveOperationAttribute(Type messagePayloadType, params string[] tags)
        {
            Action = AsyncApiAction.Receive;
            MessagePayloadType = messagePayloadType;
            Tags = tags;
        }

        public ReceiveOperationAttribute(Type messagePayloadType)
        {
            Action = AsyncApiAction.Receive;
            MessagePayloadType = messagePayloadType;
        }

        public ReceiveOperationAttribute()
        {
            Action = AsyncApiAction.Receive;
        }
    }
}
