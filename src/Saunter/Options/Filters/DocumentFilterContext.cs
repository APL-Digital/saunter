using System;
using System.Collections.Generic;

namespace Saunter.Options.Filters
{
    /// <summary>
    /// Context passed to <see cref="IDocumentFilter.Apply"/> describing the types the document was generated from.
    /// </summary>
    public class DocumentFilterContext
    {
        /// <summary>
        /// Initializes the context with the annotated types included in the document.
        /// </summary>
        /// <param name="asyncApiTypes">The types marked with <c>[AsyncApi]</c> that contributed to the document.</param>
        public DocumentFilterContext(IEnumerable<Type> asyncApiTypes)
        {
            AsyncApiTypes = asyncApiTypes;
        }

        /// <summary>
        /// The types marked with <c>[AsyncApi]</c> that contributed to the document.
        /// </summary>
        public IEnumerable<Type> AsyncApiTypes { get; }
    }
}
