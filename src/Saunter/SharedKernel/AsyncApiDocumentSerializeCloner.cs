using System.Collections.Generic;
using System.IO;
using ByteBard.AsyncAPI;
using ByteBard.AsyncAPI.Bindings;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Readers;
using Microsoft.Extensions.Logging;
using Saunter.SharedKernel.Interfaces;

namespace Saunter.SharedKernel
{
    internal class AsyncApiDocumentSerializeCloner : IAsyncApiDocumentCloner
    {
        private readonly ILogger<AsyncApiDocumentSerializeCloner> _logger;

        public AsyncApiDocumentSerializeCloner(ILogger<AsyncApiDocumentSerializeCloner> logger)
        {
            _logger = logger;
        }

        public AsyncApiDocument CloneProtype(AsyncApiDocument prototype)
        {
            var jsonView = prototype.SerializeAsJson(AsyncApiVersion.AsyncApi3_0);
            var settings = new AsyncApiReaderSettings
            {
                Bindings = BindingsCollection.All,
            };

            var reader = new AsyncApiTextReader(settings);
            var cloned = reader.Read(new StringReader(jsonView), out var diagnostic);

            if (diagnostic is not null)
            {
                _logger.LogDebug("AsyncAPI document clone completed with diagnostics.");
            }

            cloned.Components ??= new AsyncApiComponents();
            cloned.Channels ??= new Dictionary<string, AsyncApiChannel>();
            cloned.Operations ??= new Dictionary<string, AsyncApiOperation>();
            cloned.Servers ??= new Dictionary<string, AsyncApiServer>();

            return cloned;
        }
    }
}
