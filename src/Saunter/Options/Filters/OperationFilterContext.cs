using System.Reflection;
using Saunter.AttributeProvider.Attributes;

namespace Saunter.Options.Filters
{
    public class OperationFilterContext
    {
        public OperationFilterContext(MemberInfo member, OperationAttribute operation)
        {
            Member = member;
            Operation = operation;
        }

        public MemberInfo Member { get; }

        public OperationAttribute Operation { get; }
    }
}
