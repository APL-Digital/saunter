using System.Reflection;
using Saunter.AttributeProvider.Attributes;

namespace Saunter.Options.Filters
{
    /// <summary>
    /// Context passed to <see cref="IChannelFilter.Apply"/> describing where the channel was discovered.
    /// </summary>
    public class ChannelFilterContext
    {
        /// <summary>
        /// Initializes the context for a channel discovered on <paramref name="member"/>.
        /// </summary>
        /// <param name="member">The method, class or interface the channel was generated from.</param>
        /// <param name="channel">The channel attribute data the channel was generated from.</param>
        public ChannelFilterContext(MemberInfo member, ChannelAttribute channel)
        {
            Member = member;
            Channel = channel;
        }

        /// <summary>
        /// The method, class or interface the channel was generated from.
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// The channel attribute data the channel was generated from.
        /// </summary>
        public ChannelAttribute Channel { get; }
    }
}
