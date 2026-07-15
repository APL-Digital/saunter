using System.Collections.Generic;

namespace Saunter.AttributeProvider.Descriptors
{
    /// <summary>
    /// Describes an AsyncAPI <c>operation reply</c> object for request/reply operations.
    /// </summary>
    /// <param name="ChannelId">The id of the channel the reply is sent on, or <c>null</c> when the reply address is dynamic.</param>
    /// <param name="AddressLocation">The AsyncAPI <c>reply.address.location</c> field: a runtime expression locating the reply address (e.g. <c>$message.header#/replyTo</c>).</param>
    /// <param name="AddressDescription">The AsyncAPI <c>reply.address.description</c> field: an optional description of the reply address.</param>
    public sealed record AsyncApiOperationReplyDescriptor(
        string? ChannelId,
        string? AddressLocation,
        string? AddressDescription)
    {
        /// <summary>
        /// The ids of the messages (keys in components/messages) that can be sent as the reply.
        /// </summary>
        public IList<string> MessageIds { get; init; } = new List<string>();
    }
}
