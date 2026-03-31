using ByteBard.AsyncAPI.Models;

namespace Saunter.SharedKernel.Interfaces
{
    public interface IAsyncApiChannelUnion
    {
        AsyncApiChannel Union(AsyncApiChannel source, AsyncApiChannel additionaly);
    }
}
