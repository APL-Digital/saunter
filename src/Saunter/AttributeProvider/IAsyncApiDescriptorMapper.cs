using ByteBard.AsyncAPI.Models;
using Saunter.AttributeProvider.Descriptors;

namespace Saunter.AttributeProvider
{
    internal interface IAsyncApiDescriptorMapper
    {
        void RegisterMessageResolution(AsyncApiComponents components, AsyncApiMessageResolutionDescriptor resolution);

        AsyncApiChannel MapChannel(AsyncApiComponents components, AsyncApiChannelDescriptor descriptor);

        AsyncApiOperation MapOperation(AsyncApiOperationDescriptor descriptor);
    }
}
