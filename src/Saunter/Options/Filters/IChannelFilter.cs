using Saunter.AttributeProvider.Descriptors;

namespace Saunter.Options.Filters
{
    /// <summary>
    /// Post-processes each generated channel before it is added to the document.
    /// Register implementations via <see cref="AsyncApiOptions.AddAsyncApiChannelFilter{T}"/>.
    /// </summary>
    public interface IChannelFilter
    {
        /// <summary>
        /// Modifies the generated <paramref name="channel"/> in place.
        /// </summary>
        /// <param name="channel">The channel descriptor to modify.</param>
        /// <param name="context">The member and attribute the channel was generated from.</param>
        void Apply(AsyncApiChannelDescriptor channel, ChannelFilterContext context);
    }
}
