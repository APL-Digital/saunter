using System;
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
        /// Creates a server descriptor from a broker connection URI, mapping the scheme to the
        /// AsyncAPI protocol (<c>rabbitmq</c> becomes <c>amqp</c>), the host and port to
        /// <see cref="Host"/>, and the path (e.g. a RabbitMQ virtual host) to <see cref="PathName"/>.
        /// </summary>
        /// <param name="uri">The broker connection URI, e.g. <c>rabbitmq://guest:guest@localhost:5672/vhost</c>.</param>
        /// <param name="protocol">Overrides the protocol derived from the URI scheme.</param>
        public static AsyncApiServerDescriptor FromUri(Uri uri, string? protocol = null)
        {
            ArgumentNullException.ThrowIfNull(uri);

            var host = uri.IsDefaultPort || uri.Port <= 0
                ? uri.Host
                : $"{uri.Host}:{uri.Port}";
            var pathName = uri.AbsolutePath is "" or "/" ? null : uri.AbsolutePath;

            return new AsyncApiServerDescriptor
            {
                Host = host,
                PathName = pathName,
                Protocol = protocol ?? MapSchemeToProtocol(uri.Scheme),
            };
        }

        /// <summary>
        /// Creates a server descriptor from a broker connection string. See <see cref="FromUri"/>.
        /// </summary>
        /// <param name="connectionString">The broker connection string, e.g. <c>amqp://localhost:5672</c>.</param>
        /// <param name="protocol">Overrides the protocol derived from the URI scheme.</param>
        public static AsyncApiServerDescriptor FromConnectionString(string connectionString, string? protocol = null)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException(
                    $"The connection string '{connectionString}' is not a valid absolute URI.",
                    nameof(connectionString));
            }

            return FromUri(uri, protocol);
        }

        private static string MapSchemeToProtocol(string scheme)
        {
            return scheme.ToLowerInvariant() switch
            {
                "rabbitmq" or "amqp" => "amqp",
                "rabbitmqs" or "amqps" => "amqps",
                _ => scheme.ToLowerInvariant(),
            };
        }

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
