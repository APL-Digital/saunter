using Saunter.AttributeProvider.Descriptors;

namespace Saunter.SharedKernel.Interfaces
{
    internal interface IAsyncApiChannelUnion
    {
        AsyncApiChannelDescriptor Union(AsyncApiChannelDescriptor source, AsyncApiChannelDescriptor additional);
    }
}
