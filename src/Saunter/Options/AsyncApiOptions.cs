using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Saunter.Options.Filters;

namespace Saunter.Options
{
    /// <summary>
    /// Root options for Saunter's AsyncAPI document generation, middleware and UI hosting.
    /// </summary>
    public class AsyncApiOptions
    {
        private readonly List<Type> _documentFilters = new();
        private readonly List<Type> _channelFilters = new();
        private readonly List<Type> _operationFilters = new();

        /// <summary>
        /// The prototype for the default (unnamed) AsyncAPI document. Used when a document is requested
        /// without a name, or when the requested name matches neither <see cref="Documents"/> nor <see cref="NamedApis"/>.
        /// Set static document data (info, servers, components, etc.) here; channels and operations
        /// discovered from attributes are merged into a clone of this prototype.
        /// </summary>
        public AsyncApiDocumentDescriptor AsyncApi { get; set; } = new AsyncApiDocumentDescriptor();

        /// <summary>
        /// Types whose assemblies are scanned for classes and interfaces marked with
        /// <see cref="AttributeProvider.Attributes.AsyncApiAttribute"/>. One marker type per assembly is
        /// sufficient — markers identify assemblies to scan, not the types to document.
        /// When empty, the application's entry assembly is scanned.
        /// </summary>
        public IList<Type> AssemblyMarkerTypes { get; set; } = new List<Type>();

        /// <summary>
        /// Optional delegate used to pick the JSON property name for each CLR property when generating
        /// payload schemas. Defaults to camel-casing the property name when not set.
        /// </summary>
        public Func<PropertyInfo, string>? PropertyNameSelector { get; set; }

        /// <summary>
        /// Resolves the assemblies that are scanned for annotated types: the assemblies of
        /// <see cref="AssemblyMarkerTypes"/> when any are set, otherwise the entry assembly.
        /// </summary>
        internal IReadOnlyList<Assembly> GetEffectiveScanAssemblies()
        {
            if (AssemblyMarkerTypes.Count > 0)
            {
                return AssemblyMarkerTypes
                    .Select(t => t.Assembly)
                    .Distinct()
                    .ToArray();
            }

            var entryAssembly = Assembly.GetEntryAssembly();
            return entryAssembly is null ? Array.Empty<Assembly>() : new[] { entryAssembly };
        }

        internal virtual IReadOnlyCollection<TypeInfo> AsyncApiSchemaTypes => GetEffectiveScanAssemblies()
            .SelectMany(a => a.DefinedTypes)
            .ToImmutableHashSet();

        /// <summary>
        /// The <see cref="IDocumentFilter"/> types registered via <see cref="AddDocumentFilter{T}"/>,
        /// applied to each generated document after channels and operations have been built.
        /// </summary>
        public IEnumerable<Type> DocumentFilters => _documentFilters;

        /// <summary>
        /// The <see cref="IChannelFilter"/> types registered via <see cref="AddChannelFilter{T}"/>,
        /// applied to each generated channel.
        /// </summary>
        public IEnumerable<Type> ChannelFilters => _channelFilters;

        /// <summary>
        /// The <see cref="IOperationFilter"/> types registered via <see cref="AddOperationFilter{T}"/>,
        /// applied to each generated operation.
        /// </summary>
        public IEnumerable<Type> OperationFilters => _operationFilters;

        /// <summary>
        /// Registers an <see cref="IDocumentFilter"/> to post-process every generated document.
        /// The filter is resolved from the service provider, so it may take constructor dependencies.
        /// </summary>
        /// <typeparam name="T">The filter implementation to register.</typeparam>
        public void AddDocumentFilter<T>() where T : IDocumentFilter
        {
            _documentFilters.Add(typeof(T));
        }

        /// <summary>
        /// Registers an <see cref="IChannelFilter"/> to post-process every generated channel.
        /// The filter is resolved from the service provider, so it may take constructor dependencies.
        /// </summary>
        /// <typeparam name="T">The filter implementation to register.</typeparam>
        public void AddChannelFilter<T>() where T : IChannelFilter
        {
            _channelFilters.Add(typeof(T));
        }

        /// <summary>
        /// Registers an <see cref="IChannelFilter"/> to post-process every generated channel.
        /// </summary>
        /// <typeparam name="T">The filter implementation to register.</typeparam>
        [Obsolete("Use AddChannelFilter instead; it matches the AddDocumentFilter/AddOperationFilter naming.")]
        public void AddAsyncApiChannelFilter<T>() where T : IChannelFilter
        {
            AddChannelFilter<T>();
        }

        /// <summary>
        /// Registers an <see cref="IOperationFilter"/> to post-process every generated operation.
        /// The filter is resolved from the service provider, so it may take constructor dependencies.
        /// </summary>
        /// <typeparam name="T">The filter implementation to register.</typeparam>
        public void AddOperationFilter<T>() where T : IOperationFilter
        {
            _operationFilters.Add(typeof(T));
        }

        /// <summary>
        /// Route options for hosting the default document and UI. Per-document routes configured
        /// through <see cref="Documents"/> use their own <see cref="AsyncApiDocumentRegistration.Middleware"/> instead.
        /// </summary>
        public AsyncApiMiddlewareOptions Middleware { get; } = new AsyncApiMiddlewareOptions();

        /// <summary>
        /// Controls how Saunter infers ids, addresses, payload types and names that were not
        /// specified explicitly on attributes.
        /// </summary>
        public AsyncApiInferenceOptions Inference { get; } = new AsyncApiInferenceOptions();

        /// <summary>
        /// Additional named document prototypes keyed by document name, populated via
        /// <c>ConfigureNamedAsyncApi</c>. When a named document is requested, the matching prototype is used
        /// in place of <see cref="AsyncApi"/>. If the same name also exists in <see cref="Documents"/>,
        /// the <see cref="Documents"/> registration takes precedence.
        /// </summary>
        public ConcurrentDictionary<string, AsyncApiDocumentDescriptor> NamedApis { get; } = new();

        /// <summary>
        /// Full per-document registrations keyed by document name, populated via
        /// <c>ConfigureAsyncApiDocument</c>. Unlike <see cref="NamedApis"/>, a registration also carries its own
        /// routes, marker types and type filter. When both <see cref="Documents"/> and <see cref="NamedApis"/>
        /// define the same name, the registration in <see cref="Documents"/> wins.
        /// </summary>
        public ConcurrentDictionary<string, AsyncApiDocumentRegistration> Documents { get; } = new();

        /// <summary>
        /// When <c>true</c>, every registered document is generated once at application startup so
        /// misconfiguration (duplicate operation ids, unresolved references, invalid reply setups)
        /// fails fast with a descriptive exception instead of a 500 on the first request to the
        /// document endpoint. When <c>null</c> (the default), startup validation runs only in the
        /// Development environment. Set to <c>false</c> to disable entirely.
        /// </summary>
        public bool? ValidateOnStartup { get; set; }
    }
}
