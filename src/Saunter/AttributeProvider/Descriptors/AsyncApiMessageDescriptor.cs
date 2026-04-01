using System.Collections.Generic;

namespace Saunter.AttributeProvider.Descriptors
{
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
        IReadOnlyList<string> Tags);
}
