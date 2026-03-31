using System.Collections.Generic;
using System.Reflection;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;

namespace Saunter.AttributeProvider
{
    internal interface IAttributeOperationBuilder
    {
        AsyncApiOperationDescriptor Build(MemberInfo member, OperationAttribute operationAttribute, string channelId, IReadOnlyList<string> messageIds);
    }
}
