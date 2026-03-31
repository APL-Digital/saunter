using Saunter.AttributeProvider.Descriptors;
using Saunter.Options.Filters;

public interface IOperationFilter
{
    void Apply(AsyncApiOperationDescriptor operation, OperationFilterContext context);
}
