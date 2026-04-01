using System.Reflection;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;
using Saunter.Options;

namespace Saunter.AttributeProvider
{
    internal interface IAttributeMessageResolver
    {
        AsyncApiMessageResolutionDescriptor ResolveForOperation(MethodInfo method, OperationAttribute operationAttribute, AsyncApiInferenceOptions inferenceOptions);

        AsyncApiMessageResolutionDescriptor ResolveForOperation(TypeInfo type, OperationAttribute operationAttribute, AsyncApiInferenceOptions inferenceOptions);
    }
}
