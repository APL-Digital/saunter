using MassTransit;
using MassTransitStreetlights.Consumers;

namespace MassTransitStreetlights.Consumers.Definitions;

public class LightMeasuredConsumerDefinition : ConsumerDefinition<LightMeasuredConsumer>
{
    public LightMeasuredConsumerDefinition()
    {
        EndpointName = "streetlight-lighting-measured";
        ConcurrentMessageLimit = 8;
    }
}
