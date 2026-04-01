using Saunter.AttributeProvider.Descriptors;

public interface IChannelFilter
{
    void Apply(AsyncApiChannelDescriptor channel, ChannelFilterContext context);
}
