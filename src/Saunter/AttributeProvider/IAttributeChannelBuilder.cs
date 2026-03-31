using System.Collections.Generic;
using System.Reflection;
using Saunter.AttributeProvider.Attributes;
using Saunter.AttributeProvider.Descriptors;

namespace Saunter.AttributeProvider
{
    internal interface IAttributeChannelBuilder
    {
        AsyncApiChannelDescriptor Build(MemberInfo member, ChannelAttribute attribute, IReadOnlyList<string> messageIds);
    }
}
