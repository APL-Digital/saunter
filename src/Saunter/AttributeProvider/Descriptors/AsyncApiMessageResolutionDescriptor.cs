using System.Collections.Generic;

namespace Saunter.AttributeProvider.Descriptors
{
    public sealed record AsyncApiMessageResolutionDescriptor(
        IReadOnlyList<string> MessageIds,
        IReadOnlyList<AsyncApiMessageDescriptor> Messages,
        IReadOnlyList<AsyncApiSchemaComponentDescriptor> Schemas);
}
