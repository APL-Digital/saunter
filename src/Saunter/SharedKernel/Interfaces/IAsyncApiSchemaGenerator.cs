using System;
using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;

namespace Saunter.SharedKernel.Interfaces
{
    public interface IAsyncApiSchemaGenerator
    {
        GeneratedSchemas? Generate(Type? type);
    }

    public readonly record struct GeneratedSchemas(AsyncApiJsonSchema Root, IReadOnlyCollection<AsyncApiJsonSchema> All);
}
