using System;
using System.Collections.Generic;
using System.Reflection;

namespace Saunter.Options
{
    /// <summary>
    /// A self-contained registration for one hosted AsyncAPI document, created via
    /// <c>ConfigureAsyncApiDocument</c> and stored in <see cref="AsyncApiOptions.Documents"/>.
    /// It bundles the document prototype with its own routes, assembly scan scope and type filter.
    /// </summary>
    public sealed class AsyncApiDocumentRegistration
    {
        internal AsyncApiDocumentRegistration(string name)
        {
            Name = name;
            AttributeDocumentName = name;
        }

        /// <summary>
        /// The key under which this document is hosted and looked up. It is the dictionary key in
        /// <see cref="AsyncApiOptions.Documents"/>, the name passed to the document provider, and the
        /// value substituted into the default per-document routes.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The <see cref="AttributeProvider.Attributes.AsyncApiAttribute.DocumentName"/> value that annotated
        /// types must declare (e.g. <c>[AsyncApi("orders")]</c>) to be included in this document.
        /// Defaults to <see cref="Name"/>; set to <c>null</c> to select types whose attribute has no document name.
        /// </summary>
        public string? AttributeDocumentName { get; set; }

        /// <summary>
        /// The prototype for this document. Set static document data (info, servers, components, etc.) here;
        /// channels and operations discovered from attributes are merged into a clone of this prototype.
        /// </summary>
        public AsyncApiDocumentDescriptor Document { get; } = new AsyncApiDocumentDescriptor();

        /// <summary>
        /// Route options for hosting this document and its UI. Defaults to
        /// <c>/asyncapi/{name}/asyncapi.json</c> and <c>/asyncapi/{name}/ui</c>.
        /// </summary>
        public AsyncApiMiddlewareOptions Middleware { get; } = new AsyncApiMiddlewareOptions();

        /// <summary>
        /// Types whose assemblies are scanned for annotated types when building this document.
        /// When empty, the global <see cref="AsyncApiOptions.AssemblyMarkerTypes"/> scan scope is used.
        /// </summary>
        public IList<Type> MarkerTypes { get; } = new List<Type>();

        /// <summary>
        /// Optional predicate applied to each candidate annotated type; return <c>false</c> to exclude
        /// the type from this document. When <c>null</c>, all matching annotated types are included.
        /// </summary>
        public Func<TypeInfo, bool>? TypeFilter { get; set; }
    }
}
