using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Saunter.Options.Filters;

namespace Saunter.Options
{
    public class AsyncApiOptions
    {
        private readonly List<Type> _documentFilters = new();
        private readonly List<Type> _channelFilters = new();
        private readonly List<Type> _operationFilters = new();

        public AsyncApiDocumentDescriptor AsyncApi { get; set; } = new AsyncApiDocumentDescriptor();

        public IList<Type> AssemblyMarkerTypes { get; set; } = new List<Type>();

        public Func<PropertyInfo, string>? PropertyNameSelector { get; set; }

        internal virtual IReadOnlyCollection<TypeInfo> AsyncApiSchemaTypes => AssemblyMarkerTypes
            .Select(t => t.Assembly)
            .Distinct()
            .SelectMany(a => a.DefinedTypes)
            .ToImmutableHashSet();

        public IEnumerable<Type> DocumentFilters => _documentFilters;

        public IEnumerable<Type> ChannelFilters => _channelFilters;

        public IEnumerable<Type> OperationFilters => _operationFilters;

        public void AddDocumentFilter<T>() where T : IDocumentFilter
        {
            _documentFilters.Add(typeof(T));
        }

        public void AddAsyncApiChannelFilter<T>() where T : IChannelFilter
        {
            _channelFilters.Add(typeof(T));
        }

        public void AddOperationFilter<T>() where T : IOperationFilter
        {
            _operationFilters.Add(typeof(T));
        }

        public AsyncApiMiddlewareOptions Middleware { get; } = new AsyncApiMiddlewareOptions();

        public AsyncApiInferenceOptions Inference { get; } = new AsyncApiInferenceOptions();

        public ConcurrentDictionary<string, AsyncApiDocumentDescriptor> NamedApis { get; set; } = new();
    }
}
