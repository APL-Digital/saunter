namespace Saunter.AttributeProvider
{
    /// <summary>
    /// Computes a channel address at attribute-construction time. Used with the
    /// <see cref="Attributes.ChannelAttribute(string, System.Type, System.Type)"/> constructor:
    /// implementations must expose a constructor accepting the message <see cref="System.Type"/>.
    /// </summary>
    public interface IChannelResolver
    {
        /// <summary>
        /// Returns the channel address (e.g. topic or queue name) for the message type the resolver
        /// was constructed with.
        /// </summary>
        string ResolveChannelName();
    }
}
