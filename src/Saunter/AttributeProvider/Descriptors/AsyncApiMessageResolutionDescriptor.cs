using System.Collections.Generic;

namespace Saunter.AttributeProvider.Descriptors
{
    /// <summary>
    /// The result of resolving the messages for a channel member: the message ids to reference
    /// from the channel, together with the message and schema components to register.
    /// </summary>
    /// <param name="MessageIds">The ids of the resolved messages, in the order they were resolved.</param>
    /// <param name="Messages">The message descriptors to register in components/messages.</param>
    /// <param name="Schemas">The payload and header schemas to register in components/schemas.</param>
    public sealed record AsyncApiMessageResolutionDescriptor(
        IReadOnlyList<string> MessageIds,
        IReadOnlyList<AsyncApiMessageDescriptor> Messages,
        IReadOnlyList<AsyncApiSchemaComponentDescriptor> Schemas);
}
