using System.Collections.Generic;

namespace Saunter.AttributeProvider.Descriptors
{
    /// <summary>
    /// Describes an AsyncAPI <c>parameter</c> object: a <c>{parameter}</c> segment in a channel address.
    /// </summary>
    /// <param name="Name">The parameter name as it appears in the channel address (without braces).</param>
    /// <param name="Description">The AsyncAPI <c>parameter.description</c> field: an optional description of the parameter.</param>
    /// <param name="Location">The AsyncAPI <c>parameter.location</c> field: a runtime expression locating the parameter value (e.g. <c>$message.header#/id</c>).</param>
    /// <param name="EnumValues">The AsyncAPI <c>parameter.enum</c> field: the set of values the parameter is limited to.</param>
    /// <param name="DefaultValue">The AsyncAPI <c>parameter.default</c> field: the value to use when none is supplied.</param>
    /// <param name="Examples">The AsyncAPI <c>parameter.examples</c> field: example values for the parameter.</param>
    public sealed record AsyncApiParameterDescriptor(
        string Name,
        string? Description,
        string? Location,
        IReadOnlyList<string> EnumValues,
        string? DefaultValue,
        IReadOnlyList<string> Examples);
}
