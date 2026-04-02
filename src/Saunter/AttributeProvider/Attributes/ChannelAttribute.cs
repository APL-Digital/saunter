using System;

namespace Saunter.AttributeProvider.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
    public class ChannelAttribute : Attribute
    {
        /// <summary>
        /// Used as the generated AsyncAPI v3 channel key. When omitted, Saunter can infer it from the address.
        /// </summary>
        public string ChannelId { get; set; }

        public string Address { get; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        public string? Description { get; set; }

        public string? BindingsRef { get; set; }

        public string[] Tags { get; set; }

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

        public ChannelAttribute(string address)
        {
            ArgumentNullException.ThrowIfNull(address);

            ChannelId = string.Empty;
            Address = address;
            Tags = Array.Empty<string>();
            Servers = Array.Empty<string>();
        }

        public ChannelAttribute(string channelId, string address)
        {
            ArgumentNullException.ThrowIfNull(channelId);
            ArgumentNullException.ThrowIfNull(address);

            ChannelId = channelId;
            Address = address;
            Tags = Array.Empty<string>();
            Servers = Array.Empty<string>();
        }

        public ChannelAttribute(string channelId, Type resolverType, Type messageType)
        {
            ArgumentNullException.ThrowIfNull(channelId);
            ArgumentNullException.ThrowIfNull(resolverType);
            ArgumentNullException.ThrowIfNull(messageType);

            IChannelResolver resolver = Activator.CreateInstance(resolverType, messageType) as IChannelResolver
                ?? throw new ArgumentException("resolverType must implement IChannelResolver", nameof(resolverType));

            ChannelId = channelId;
            Address = resolver.ResolveChannelName()
                ?? throw new InvalidOperationException($"IChannelResolver '{resolverType.Name}' returned null for channel address.");
            Tags = Array.Empty<string>();
            Servers = Array.Empty<string>();
        }
    }
}
