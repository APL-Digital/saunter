using System.Collections.Generic;

namespace Saunter.AttributeProvider.Descriptors
{
    public sealed record AsyncApiParameterDescriptor(
        string Name,
        string? Description,
        string? Location,
        IReadOnlyList<string> EnumValues,
        string? DefaultValue,
        IReadOnlyList<string> Examples);
}
