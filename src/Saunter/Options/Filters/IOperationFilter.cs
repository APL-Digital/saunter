using Saunter.AttributeProvider.Descriptors;

namespace Saunter.Options.Filters
{
    public interface IOperationFilter
    {
        void Apply(AsyncApiOperationDescriptor operation, OperationFilterContext context);
    }
}
