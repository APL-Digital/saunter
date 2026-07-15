namespace Saunter.Options.Filters
{
    /// <summary>
    /// Post-processes each generated document after all channels and operations have been built.
    /// Register implementations via <see cref="AsyncApiOptions.AddDocumentFilter{T}"/>.
    /// </summary>
    public interface IDocumentFilter
    {
        /// <summary>
        /// Modifies the generated <paramref name="document"/> in place.
        /// </summary>
        /// <param name="document">The document descriptor to modify.</param>
        /// <param name="context">The annotated types the document was generated from.</param>
        void Apply(AsyncApiDocumentDescriptor document, DocumentFilterContext context);
    }
}
