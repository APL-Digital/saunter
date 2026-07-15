using System;

namespace Saunter.AttributeProvider.Attributes
{
    /// <summary>
    /// Marks a class or interface as containing asyncapi channels.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class AsyncApiAttribute : Attribute
    {
        /// <summary>
        /// The name of the document this type belongs to. Matched against the requested document name
        /// (or a registration's <c>AttributeDocumentName</c>) during assembly scanning; <c>null</c> assigns
        /// the type to the default (unnamed) document.
        /// </summary>
        public string? DocumentName { get; }

        /// <summary>
        /// Marks the type as containing AsyncAPI channels, optionally assigning it to a named document.
        /// </summary>
        /// <param name="documentName">The document the type belongs to, or <c>null</c> for the default document.</param>
        public AsyncApiAttribute(string? documentName = null)
        {
            DocumentName = documentName;
        }
    }
}
