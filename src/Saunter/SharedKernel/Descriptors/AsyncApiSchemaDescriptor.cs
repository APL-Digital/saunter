using System.Collections.Generic;

namespace Saunter.SharedKernel.Descriptors
{
    public enum AsyncApiSchemaValueType
    {
        Boolean,
        Integer,
        Number,
        String,
        Object,
        Array,
    }

    public sealed class AsyncApiSchemaDescriptor
    {
        public string? Id { get; set; }

        public AsyncApiSchemaValueType? Type { get; set; }

        public string? Format { get; set; }

        public bool Nullable { get; set; }

        public string? Reference { get; set; }

        public AsyncApiSchemaDescriptor? Items { get; set; }

        public IDictionary<string, AsyncApiSchemaDescriptor> Properties { get; } = new Dictionary<string, AsyncApiSchemaDescriptor>();

        public IList<string> Required { get; } = new List<string>();

        public IList<string> EnumValues { get; } = new List<string>();

        public IList<AsyncApiSchemaDescriptor> OneOf { get; } = new List<AsyncApiSchemaDescriptor>();

        public IList<AsyncApiSchemaDescriptor> AllOf { get; } = new List<AsyncApiSchemaDescriptor>();
    }

    internal readonly record struct GeneratedSchemaDescriptors(
        AsyncApiSchemaDescriptor Root,
        IReadOnlyCollection<AsyncApiSchemaDescriptor> All);
}
