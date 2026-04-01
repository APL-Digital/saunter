using Saunter;

namespace Saunter.SharedKernel.Interfaces
{
    public interface IAsyncApiDocumentCloner
    {
        AsyncApiDocumentDescriptor ClonePrototype(AsyncApiDocumentDescriptor prototype);
    }
}
