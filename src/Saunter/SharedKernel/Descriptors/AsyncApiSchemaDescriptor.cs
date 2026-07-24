using System.Collections.Generic;

namespace Saunter.SharedKernel.Descriptors
{
    /// <summary>
    /// The JSON Schema value type of a schema (the schema's <c>type</c> keyword).
    /// </summary>
    public enum AsyncApiSchemaValueType
    {
        /// <summary>The JSON Schema <c>boolean</c> type.</summary>
        Boolean,

        /// <summary>The JSON Schema <c>integer</c> type.</summary>
        Integer,

        /// <summary>The JSON Schema <c>number</c> type.</summary>
        Number,

        /// <summary>The JSON Schema <c>string</c> type.</summary>
        String,

        /// <summary>The JSON Schema <c>object</c> type.</summary>
        Object,

        /// <summary>The JSON Schema <c>array</c> type.</summary>
        Array,
    }

    /// <summary>
    /// Describes a JSON Schema generated from a CLR type, used for message payloads and headers.
    /// </summary>
    public sealed class AsyncApiSchemaDescriptor
    {
        /// <summary>
        /// The schema key in components/schemas, or <c>null</c> for inline schemas
        /// (e.g. array item or primitive schemas).
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The JSON Schema <c>type</c> keyword, or <c>null</c> when the schema is a pure reference.
        /// </summary>
        public AsyncApiSchemaValueType? Type { get; set; }

        /// <summary>
        /// The JSON Schema <c>format</c> keyword (e.g. <c>int32</c>, <c>date-time</c>, <c>uuid</c>).
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Whether the value may be <c>null</c>; emitted as a nullable type in the generated schema.
        /// </summary>
        public bool Nullable { get; set; }

        /// <summary>
        /// The id of another schema in components/schemas this schema refers to
        /// (emitted as a <c>$ref</c>), or <c>null</c> when the schema is defined inline.
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// The schema of array elements (JSON Schema <c>items</c>); only set when <see cref="Type"/> is <see cref="AsyncApiSchemaValueType.Array"/>.
        /// </summary>
        public AsyncApiSchemaDescriptor? Items { get; set; }

        /// <summary>
        /// The schema of dictionary values (JSON Schema <c>additionalProperties</c>), or <c>null</c>.
        /// </summary>
        public AsyncApiSchemaDescriptor? AdditionalProperties { get; set; }

        /// <summary>
        /// The object property schemas keyed by property name (JSON Schema <c>properties</c>).
        /// </summary>
        public IDictionary<string, AsyncApiSchemaDescriptor> Properties { get; } = new Dictionary<string, AsyncApiSchemaDescriptor>();

        /// <summary>
        /// The names of the required properties (JSON Schema <c>required</c>).
        /// </summary>
        public IList<string> Required { get; } = new List<string>();

        /// <summary>
        /// The allowed values for enum schemas (JSON Schema <c>enum</c>).
        /// </summary>
        public IList<string> EnumValues { get; } = new List<string>();

        /// <summary>
        /// Alternative schemas of which exactly one must match (JSON Schema <c>oneOf</c>).
        /// </summary>
        public IList<AsyncApiSchemaDescriptor> OneOf { get; } = new List<AsyncApiSchemaDescriptor>();

        /// <summary>
        /// Schemas that must all match (JSON Schema <c>allOf</c>); the generator uses this to
        /// wrap a <c>$ref</c> together with extra keywords such as nullability.
        /// </summary>
        public IList<AsyncApiSchemaDescriptor> AllOf { get; } = new List<AsyncApiSchemaDescriptor>();
    }

    /// <summary>
    /// The result of generating schemas for a CLR type.
    /// </summary>
    /// <param name="Root">The schema describing the type itself, as used at its usage site.</param>
    /// <param name="All">All reusable component schemas (including <paramref name="Root"/> when it has an id) discovered while walking the type graph.</param>
    public readonly record struct GeneratedSchemaDescriptors(
        AsyncApiSchemaDescriptor Root,
        IReadOnlyCollection<AsyncApiSchemaDescriptor> All);
}
