using System;
using Saunter.SharedKernel.Descriptors;

namespace Saunter.SharedKernel.Interfaces
{
    internal interface IAsyncApiSchemaGenerator
    {
        GeneratedSchemaDescriptors? Generate(Type? type);
    }
}
