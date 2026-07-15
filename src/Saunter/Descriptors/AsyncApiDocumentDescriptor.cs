using System.Collections.Generic;

namespace Saunter
{
    /// <summary>
    /// Describes an AsyncAPI 3.0 document. Used both as a user-configured prototype
    /// (static info, servers and components) and as the fully generated document that Saunter
    /// serializes, after attribute-discovered channels and operations have been merged in.
    /// </summary>
    public class AsyncApiDocumentDescriptor
    {
        /// <summary>
        /// The AsyncAPI <c>id</c> field: a unique identifier (URI) for the application being described.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The AsyncAPI <c>asyncapi</c> version string. When unset, Saunter emits <c>3.0.0</c>.
        /// </summary>
        public string? Asyncapi { get; set; }

        /// <summary>
        /// The AsyncAPI <c>info</c> object: metadata about the API such as title, version and contact.
        /// </summary>
        public AsyncApiInfoDescriptor Info { get; set; } = new();

        /// <summary>
        /// The AsyncAPI <c>defaultContentType</c> field: the content type used for message payloads that
        /// do not declare one. Defaults to <c>application/json</c> when auto-set is enabled in the inference options.
        /// </summary>
        public string? DefaultContentType { get; set; }

        /// <summary>
        /// The AsyncAPI <c>components</c> object holding reusable schemas, messages, bindings and other items.
        /// </summary>
        public AsyncApiComponentsDescriptor Components { get; set; } = new();

        /// <summary>
        /// The AsyncAPI <c>servers</c> map: message brokers or other servers, keyed by server name.
        /// </summary>
        public IDictionary<string, AsyncApiServerDescriptor> Servers { get; set; } = new Dictionary<string, AsyncApiServerDescriptor>();

        /// <summary>
        /// The AsyncAPI <c>channels</c> map, keyed by channel id. Populated from attribute discovery
        /// and merged with any preconfigured channels.
        /// </summary>
        public IDictionary<string, AttributeProvider.Descriptors.AsyncApiChannelDescriptor> Channels { get; set; } = new Dictionary<string, AttributeProvider.Descriptors.AsyncApiChannelDescriptor>();

        /// <summary>
        /// The AsyncAPI <c>operations</c> map, keyed by operation id. Populated from attribute discovery
        /// and merged with any preconfigured operations.
        /// </summary>
        public IDictionary<string, AttributeProvider.Descriptors.AsyncApiOperationDescriptor> Operations { get; set; } = new Dictionary<string, AttributeProvider.Descriptors.AsyncApiOperationDescriptor>();
    }
}
