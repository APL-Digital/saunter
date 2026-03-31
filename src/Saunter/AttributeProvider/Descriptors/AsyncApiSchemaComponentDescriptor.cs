using Saunter.SharedKernel.Descriptors;

namespace Saunter.AttributeProvider.Descriptors
{
    public sealed record AsyncApiSchemaComponentDescriptor(
        string Id,
        AsyncApiSchemaDescriptor Schema);
}
