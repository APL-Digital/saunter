using ByteBard.AsyncAPI.Models;
using Saunter.SharedKernel.Descriptors;

namespace Saunter.SharedKernel.Interfaces
{
    internal interface IAsyncApiSchemaMapper
    {
        AsyncApiJsonSchema Map(AsyncApiSchemaDescriptor descriptor);
    }
}
