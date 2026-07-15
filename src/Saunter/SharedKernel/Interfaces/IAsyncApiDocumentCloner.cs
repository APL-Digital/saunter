using Saunter;

namespace Saunter.SharedKernel.Interfaces
{
    /// <summary>
    /// Creates deep copies of document descriptors so that a user-configured prototype is never
    /// mutated by document generation.
    /// </summary>
    public interface IAsyncApiDocumentCloner
    {
        /// <summary>
        /// Returns a deep copy of <paramref name="prototype"/> that generation can mutate freely.
        /// </summary>
        /// <param name="prototype">The configured document prototype to copy.</param>
        AsyncApiDocumentDescriptor ClonePrototype(AsyncApiDocumentDescriptor prototype);
    }
}
