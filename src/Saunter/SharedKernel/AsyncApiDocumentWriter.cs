using ByteBard.AsyncAPI;
using ByteBard.AsyncAPI.Models;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiDocumentWriter : IAsyncApiDocumentWriter
    {
        private readonly IAsyncApiDocumentMapper _documentMapper;

        public AsyncApiDocumentWriter()
            : this(new AsyncApiDocumentMapper(new AttributeProvider.AsyncApiDescriptorMapper(new AsyncApiSchemaMapper())))
        {
        }

        public AsyncApiDocumentWriter(IAsyncApiDocumentMapper documentMapper)
        {
            _documentMapper = documentMapper;
        }

        public string WriteJson(AsyncApiDocumentDescriptor document)
        {
            var mapped = _documentMapper.Map(document);

            var serializerVersion = mapped.Asyncapi?.StartsWith("2.") == true
                ? AsyncApiVersion.AsyncApi2_0
                : AsyncApiVersion.AsyncApi3_0;

            return mapped.SerializeAsJson(serializerVersion);
        }
    }
}
