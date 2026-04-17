using System;
using System.Collections.Generic;
using System.Reflection;

namespace Saunter.Options
{
    public sealed class AsyncApiDocumentRegistration
    {
        internal AsyncApiDocumentRegistration(string name)
        {
            Name = name;
            AttributeDocumentName = name;
        }

        public string Name { get; }

        public string? AttributeDocumentName { get; set; }

        public AsyncApiDocumentDescriptor Document { get; } = new AsyncApiDocumentDescriptor();

        public AsyncApiMiddlewareOptions Middleware { get; } = new AsyncApiMiddlewareOptions();

        public IList<Type> MarkerTypes { get; } = new List<Type>();

        public Func<TypeInfo, bool>? TypeFilter { get; set; }
    }
}
