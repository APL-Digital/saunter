using System.Collections.Generic;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;

namespace Saunter
{
    /// <summary>
    /// Describes an AsyncAPI <c>server</c> object: a message broker or other server the application connects to.
    /// </summary>
    public class AsyncApiServerDescriptor
    {
        /// <summary>
        /// The AsyncAPI <c>server.host</c> field: the server host name, optionally including the port.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// The AsyncAPI <c>server.pathname</c> field: the path on the host where the server is available.
        /// </summary>
        public string? PathName { get; set; }

        /// <summary>
        /// The AsyncAPI <c>server.title</c> field: a human-friendly title for the server.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The AsyncAPI <c>server.summary</c> field: a short summary of the server.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// The AsyncAPI <c>server.description</c> field: an optional description of the server.
        /// CommonMark syntax can be used for rich text representation.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The AsyncAPI <c>server.protocol</c> field: the protocol used to connect to the server (e.g. <c>amqp</c>, <c>kafka</c>).
        /// </summary>
        public string? Protocol { get; set; }

        /// <summary>
        /// The AsyncAPI <c>server.protocolVersion</c> field: the version of the protocol used for the connection.
        /// </summary>
        public string? ProtocolVersion { get; set; }

        /// <summary>
        /// An absolute URL for additional server documentation (AsyncAPI <c>server.externalDocs.url</c>).
        /// </summary>
        public string? ExternalDocs { get; set; }

        /// <summary>
        /// Optional description for the external docs link (AsyncAPI <c>server.externalDocs.description</c>).
        /// </summary>
        public string? ExternalDocsDescription { get; set; }

        /// <summary>
        /// The name of a server bindings item to reference.
        /// The bindings must be added to components/serverBindings with the same name.
        /// </summary>
        public string? BindingsRef { get; set; }

        /// <summary>
        /// The AsyncAPI <c>server.tags</c> field: a list of tags for logical grouping of servers.
        /// </summary>
        public IList<AsyncApiTag> Tags { get; set; } = new List<AsyncApiTag>();

        /// <summary>
        /// The AsyncAPI <c>server.variables</c> map: variables used in the host or pathname, keyed by variable name.
        /// </summary>
        public IDictionary<string, AsyncApiServerVariableDescriptor> Variables { get; set; } = new Dictionary<string, AsyncApiServerVariableDescriptor>();

        /// <summary>
        /// The AsyncAPI <c>server.security</c> field: the security schemes that may be used to connect to the server.
        /// </summary>
        public IList<AsyncApiSecurityScheme> Security { get; set; } = new List<AsyncApiSecurityScheme>();

        /// <summary>
        /// The AsyncAPI <c>server.bindings</c> object: protocol-specific server bindings declared inline.
        /// </summary>
        public AsyncApiBindings<IServerBinding> Bindings { get; set; } = new AsyncApiBindings<IServerBinding>();
    }

    /// <summary>
    /// Describes an AsyncAPI <c>server variable</c> object: a variable used in a server host or pathname.
    /// </summary>
    public class AsyncApiServerVariableDescriptor
    {
        /// <summary>
        /// The AsyncAPI <c>serverVariable.default</c> field: the value to use when no other value is supplied.
        /// </summary>
        public string? Default { get; set; }

        /// <summary>
        /// The AsyncAPI <c>serverVariable.description</c> field: an optional description of the variable.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The AsyncAPI <c>serverVariable.enum</c> field: the set of values the variable is limited to.
        /// </summary>
        public IList<string> Enum { get; set; } = new List<string>();

        /// <summary>
        /// The AsyncAPI <c>serverVariable.examples</c> field: example values for the variable.
        /// </summary>
        public IList<string> Examples { get; set; } = new List<string>();
    }
}
