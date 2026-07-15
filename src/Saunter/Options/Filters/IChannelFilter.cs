using Saunter.AttributeProvider.Descriptors;

namespace Saunter.Options.Filters
{
    public interface IChannelFilter
    {
        void Apply(AsyncApiChannelDescriptor channel, ChannelFilterContext context);
    }
}
