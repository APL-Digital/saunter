using System.Reflection;
using Saunter.AttributeProvider.Attributes;

namespace Saunter.Options.Filters
{
    /// <summary>
    /// Context passed to <see cref="IOperationFilter.Apply"/> describing where the operation was discovered.
    /// </summary>
    public class OperationFilterContext
    {
        /// <summary>
        /// Initializes the context for an operation discovered on <paramref name="member"/>.
        /// </summary>
        /// <param name="member">The method, class or interface the operation was generated from.</param>
        /// <param name="operation">The operation attribute the operation was generated from.</param>
        public OperationFilterContext(MemberInfo member, OperationAttribute operation)
        {
            Member = member;
            Operation = operation;
        }

        /// <summary>
        /// The method, class or interface the operation was generated from.
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// The operation attribute the operation was generated from.
        /// </summary>
        public OperationAttribute Operation { get; }
    }
}
