using System.Collections.Generic;

namespace Saunter
{
    public class AsyncApiDocumentDescriptor
    {
        public string? Id { get; set; }

        public string? Asyncapi { get; set; }

        public AsyncApiInfoDescriptor Info { get; set; } = new();

        public string? DefaultContentType { get; set; }

        public AsyncApiComponentsDescriptor Components { get; set; } = new();

        public IDictionary<string, AsyncApiServerDescriptor> Servers { get; set; } = new Dictionary<string, AsyncApiServerDescriptor>();

        public IDictionary<string, AttributeProvider.Descriptors.AsyncApiChannelDescriptor> Channels { get; set; } = new Dictionary<string, AttributeProvider.Descriptors.AsyncApiChannelDescriptor>();

        public IDictionary<string, AttributeProvider.Descriptors.AsyncApiOperationDescriptor> Operations { get; set; } = new Dictionary<string, AttributeProvider.Descriptors.AsyncApiOperationDescriptor>();
    }
}
