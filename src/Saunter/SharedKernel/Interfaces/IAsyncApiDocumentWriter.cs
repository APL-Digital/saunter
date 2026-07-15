namespace Saunter.SharedKernel.Interfaces
{
    /// <summary>
    /// Serializes generated document descriptors to AsyncAPI JSON or YAML.
    /// </summary>
    public interface IAsyncApiDocumentWriter
    {
        /// <summary>
        /// Serializes <paramref name="document"/> to its AsyncAPI JSON representation.
        /// </summary>
        /// <param name="document">The generated document to serialize.</param>
        /// <returns>The AsyncAPI document as a JSON string.</returns>
        string WriteJson(AsyncApiDocumentDescriptor document);

        /// <summary>
        /// Serializes <paramref name="document"/> to its AsyncAPI YAML representation.
        /// </summary>
        /// <param name="document">The generated document to serialize.</param>
        /// <returns>The AsyncAPI document as a YAML string.</returns>
        string WriteYaml(AsyncApiDocumentDescriptor document);
    }
}
