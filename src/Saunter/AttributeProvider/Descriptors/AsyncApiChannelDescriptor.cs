using System.Collections.Generic;
using System.Linq;
using ByteBard.AsyncAPI.Models;

namespace Saunter.AttributeProvider.Descriptors
{
    public sealed record AsyncApiChannelDescriptor(
        string Id,
        string? Address,
        string? Title,
        string? Summary,
        string? Description,
        string? BindingsRef,
        IReadOnlyList<string> ServerNames,
        IReadOnlyList<string> MessageIds,
        IReadOnlyList<AsyncApiParameterDescriptor> Parameters)
    {
        public IList<AsyncApiTag> Tags { get; } = new List<AsyncApiTag>();

        public IReadOnlyDictionary<string, string> Messages => MessageIds.ToDictionary(id => id, id => id);

        public IReadOnlyList<string> Servers => ServerNames;
    }
}
