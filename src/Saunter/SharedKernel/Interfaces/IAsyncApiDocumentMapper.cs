using ByteBard.AsyncAPI.Models;

namespace Saunter.SharedKernel.Interfaces
{
    internal interface IAsyncApiDocumentMapper
    {
        AsyncApiDocument Map(AsyncApiDocumentDescriptor document);
    }
}
