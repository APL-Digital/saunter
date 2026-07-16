using System;

namespace Saunter.Options.Filters
{
    /// <summary>
    /// Describes a registered filter: either a type resolved through the service provider
    /// (falling back to constructor injection via <c>ActivatorUtilities</c>), or a
    /// pre-constructed instance used as-is.
    /// </summary>
    public sealed class FilterDescriptor
    {
        internal FilterDescriptor(Type filterType)
        {
            FilterType = filterType;
        }

        internal FilterDescriptor(object filterInstance)
        {
            FilterInstance = filterInstance;
        }

        /// <summary>
        /// The filter implementation type. <c>null</c> when the filter was registered as an instance.
        /// </summary>
        public Type? FilterType { get; }

        /// <summary>
        /// The pre-constructed filter instance. <c>null</c> when the filter was registered by type.
        /// </summary>
        public object? FilterInstance { get; }
    }
}
