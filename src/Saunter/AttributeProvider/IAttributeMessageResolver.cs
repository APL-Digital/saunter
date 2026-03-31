using System.Reflection;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;

namespace Saunter.AttributeProvider
{
    internal interface IAttributeMessageResolver
    {
        AsyncApiMessageResolutionDescriptor ResolveForOperation(MethodInfo method, OperationAttribute operationAttribute);

        AsyncApiMessageResolutionDescriptor ResolveForOperation(TypeInfo type, OperationAttribute operationAttribute);
    }
}
