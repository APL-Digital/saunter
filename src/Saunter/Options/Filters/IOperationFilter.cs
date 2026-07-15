using Saunter.AttributeProvider.Descriptors;

namespace Saunter.Options.Filters
{
    /// <summary>
    /// Post-processes each generated operation before it is added to the document.
    /// Register implementations via <see cref="AsyncApiOptions.AddOperationFilter{T}"/>.
    /// </summary>
    public interface IOperationFilter
    {
        /// <summary>
        /// Modifies the generated <paramref name="operation"/> in place.
        /// </summary>
        /// <param name="operation">The operation descriptor to modify.</param>
        /// <param name="context">The member and attribute the operation was generated from.</param>
        void Apply(AsyncApiOperationDescriptor operation, OperationFilterContext context);
    }
}
