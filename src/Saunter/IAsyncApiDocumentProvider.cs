using Saunter.Options;

namespace Saunter
{
    public interface IAsyncApiDocumentProvider
    {
        AsyncApiDocumentDescriptor GetDocument(string? documentName, AsyncApiOptions options);
    }
}
