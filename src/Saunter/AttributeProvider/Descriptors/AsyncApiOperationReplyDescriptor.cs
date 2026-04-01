using System.Collections.Generic;

namespace Saunter.AttributeProvider.Descriptors
{
    public sealed record AsyncApiOperationReplyDescriptor(
        string? ChannelId,
        string? AddressLocation,
        string? AddressDescription)
    {
        public IList<string> MessageIds { get; init; } = new List<string>();
    }
}
