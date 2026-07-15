using System;

namespace Saunter.AttributeProvider.Attributes
{
    /// <summary>
    /// Declares an AsyncAPI channel on a method, class or interface within a type marked with
    /// <see cref="AsyncApiAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
    public class ChannelAttribute : Attribute
    {
        /// <summary>
        /// Used as the generated AsyncAPI v3 channel key. When omitted, Saunter can infer it from the address.
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// The AsyncAPI <c>channel.address</c> field: the topic, queue or route the channel is bound to.
        /// May contain <c>{parameter}</c> segments described with <see cref="ChannelParameterAttribute"/>.
        /// When empty, the address can be inferred from ASP.NET Core route metadata if enabled.
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// A human-friendly title for the channel.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// A short summary of the channel.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// An optional description of the channel.
        /// CommonMark syntax can be used for rich text representation.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The name of a channel bindings item to reference.
        /// The bindings must be added to components/channelBindings with the same name.
        /// </summary>
        public string? BindingsRef { get; set; }

        /// <summary>
        /// A list of tags for API documentation control. Tags can be used for logical grouping of channels.
        /// </summary>
        public string[] Tags { get; set; }

        /// <summary>
        /// The names of the servers (keys in the document's <c>servers</c> map) the channel is available on.
        /// Empty means the channel is available on all servers.
        /// </summary>
        public string[] Servers { get; set; }

        /// <summary>
        /// Initializes a <see cref="ChannelAttribute"/> without an explicit address or channel id.
        /// <see cref="ChannelAttribute()"/> should only be used when the inference options passed to Build()
        /// can supply the missing values; otherwise ResolveAddress and ResolveChannelId will throw.
        /// </summary>
        public ChannelAttribute()
        {
            ChannelId = string.Empty;
            Address = string.Empty;
            Tags = Array.Empty<string>();
            Servers = Array.Empty<string>();
        }

        /// <summary>
        /// Initializes a <see cref="ChannelAttribute"/> with the given address. The channel id is
        /// inferred from the address (or must be set via <see cref="ChannelId"/>).
        /// </summary>
        /// <param name="address">The channel address.</param>
        public ChannelAttribute(string address)
        {
            ArgumentNullException.ThrowIfNull(address);

            ChannelId = string.Empty;
            Address = address;
            Tags = Array.Empty<string>();
            Servers = Array.Empty<string>();
        }

        /// <summary>
        /// Initializes a <see cref="ChannelAttribute"/> with an explicit channel id and address.
        /// </summary>
        /// <param name="channelId">The channel key in the generated document's <c>channels</c> map.</param>
        /// <param name="address">The channel address.</param>
        public ChannelAttribute(string channelId, string address)
        {
            ArgumentNullException.ThrowIfNull(channelId);
            ArgumentNullException.ThrowIfNull(address);

            ChannelId = channelId;
            Address = address;
            Tags = Array.Empty<string>();
            Servers = Array.Empty<string>();
        }

        /// <summary>
        /// Initializes a <see cref="ChannelAttribute"/> whose address is produced at attribute-construction
        /// time by an <see cref="IChannelResolver"/>. The resolver type is instantiated with
        /// <paramref name="messageType"/> as its single constructor argument and its
        /// <see cref="IChannelResolver.ResolveChannelName"/> result is used as the address.
        /// </summary>
        /// <param name="channelId">The channel key in the generated document's <c>channels</c> map.</param>
        /// <param name="resolverType">A type implementing <see cref="IChannelResolver"/> with a constructor accepting <paramref name="messageType"/>.</param>
        /// <param name="messageType">The message type passed to the resolver's constructor.</param>
        public ChannelAttribute(string channelId, Type resolverType, Type messageType)
        {
            ArgumentNullException.ThrowIfNull(channelId);
            ArgumentNullException.ThrowIfNull(resolverType);
            ArgumentNullException.ThrowIfNull(messageType);

            var resolverErrorMessage = $"Channel resolver type '{resolverType.FullName}' must implement IChannelResolver and expose a constructor that accepts message type '{messageType.FullName}'.";
            object? resolverInstance;
            try
            {
                resolverInstance = Activator.CreateInstance(resolverType, messageType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    resolverErrorMessage,
                    nameof(resolverType),
                    ex is System.Reflection.TargetInvocationException { InnerException: not null } invocationException
                        ? invocationException.InnerException
                        : ex);
            }

            IChannelResolver resolver = resolverInstance as IChannelResolver
                ?? throw new ArgumentException(resolverErrorMessage, nameof(resolverType));

            ChannelId = channelId;
            Address = resolver.ResolveChannelName()
                ?? throw new InvalidOperationException(
                    $"Channel resolver '{resolverType.FullName}' returned null for channel id '{channelId}' and message type '{messageType.FullName}'.");
            Tags = Array.Empty<string>();
            Servers = Array.Empty<string>();
        }
    }
}
