using Saunter.SharedKernel.Descriptors;

namespace Saunter.AttributeProvider.Descriptors
{
    /// <summary>
    /// A schema generated for a message payload or headers type, paired with the id
    /// under which it is registered in components/schemas.
    /// </summary>
    /// <param name="Id">The schema key in components/schemas.</param>
    /// <param name="Schema">The generated schema.</param>
    public sealed record AsyncApiSchemaComponentDescriptor(
        string Id,
        AsyncApiSchemaDescriptor Schema);
}
