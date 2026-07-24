namespace Saunter.AttributeProvider
{
    /// <summary>
    /// Validates the referential integrity of a generated AsyncAPI document descriptor.
    /// </summary>
    public interface IAsyncApiDocumentValidator
    {
        /// <summary>
        /// Validates <paramref name="document"/>, checking that all channel, message, server,
        /// binding, correlation id, trait and reply references resolve to declared components.
        /// </summary>
        /// <param name="document">The document descriptor to validate.</param>
        /// <exception cref="System.InvalidOperationException">A reference does not resolve or conflicting sources are set.</exception>
        void Validate(AsyncApiDocumentDescriptor document);
    }
}
