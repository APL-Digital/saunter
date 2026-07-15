using Saunter.Options;

namespace Saunter
{
    /// <summary>
    /// Generates AsyncAPI documents. The default implementation builds the document by scanning
    /// assemblies for attribute-annotated types and merging the results into the configured prototype.
    /// </summary>
    public interface IAsyncApiDocumentProvider
    {
        /// <summary>
        /// Generates the document with the given name.
        /// </summary>
        /// <param name="documentName">The document name to generate, or <c>null</c> for the default (unnamed) document.</param>
        /// <param name="options">The options holding the document prototypes and generation settings.</param>
        /// <returns>The fully generated document, ready to be serialized.</returns>
        AsyncApiDocumentDescriptor GetDocument(string? documentName, AsyncApiOptions options);
    }
}
