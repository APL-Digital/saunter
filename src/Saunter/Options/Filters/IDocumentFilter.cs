namespace Saunter.Options.Filters
{
    public interface IDocumentFilter
    {
        void Apply(AsyncApiDocumentDescriptor document, DocumentFilterContext context);
    }
}
