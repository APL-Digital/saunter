using System;
using Saunter.SharedKernel.Descriptors;

namespace Saunter.SharedKernel.Interfaces
{
    /// <summary>
    /// Generates JSON Schema descriptors from CLR types for use in AsyncAPI message payloads and headers.
    /// </summary>
    public interface IAsyncApiSchemaGenerator
    {
        /// <summary>
        /// Generates the schema descriptors for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type to generate a schema for.</param>
        /// <returns>
        /// The generated root schema together with all reusable component schemas discovered
        /// while walking the type graph, or <c>null</c> when <paramref name="type"/> is <c>null</c>.
        /// </returns>
        GeneratedSchemaDescriptors? Generate(Type? type);
    }
}
