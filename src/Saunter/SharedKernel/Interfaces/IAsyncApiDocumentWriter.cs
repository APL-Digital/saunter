namespace Saunter.SharedKernel.Interfaces
{
    public interface IAsyncApiDocumentWriter
    {
        string WriteJson(AsyncApiDocumentDescriptor document);
    }
}
