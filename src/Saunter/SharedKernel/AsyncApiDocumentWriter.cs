using System;
using ByteBard.AsyncAPI;
using ByteBard.AsyncAPI.Models;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiDocumentWriter : IAsyncApiDocumentWriter
    {
        private readonly IAsyncApiDocumentMapper _documentMapper;

        public AsyncApiDocumentWriter(IAsyncApiDocumentMapper documentMapper)
        {
            _documentMapper = documentMapper;
        }

        public string WriteJson(AsyncApiDocumentDescriptor document)
        {
            var mapped = _documentMapper.Map(document);

            var serializerVersion = mapped.Asyncapi switch
            {
                string version when version.StartsWith("2.") => AsyncApiVersion.AsyncApi2_0,
                string version when version.StartsWith("3.") => AsyncApiVersion.AsyncApi3_0,
                _ => throw new InvalidOperationException($"Unsupported AsyncAPI version '{mapped.Asyncapi ?? "<null>"}'.")
            };

            return mapped.SerializeAsJson(serializerVersion);
        }
    }
}
