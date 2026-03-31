using MassTransit;
using MassTransitMinimal.Contracts;
using Saunter.AttributeProvider.Attributes;
using System.Threading.Tasks;

namespace MassTransitMinimal.Producers;

[AsyncApi]
public class OrderSubmittedPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderSubmittedPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // Minimal happy path:
    // - channel id is inferred from the address
    // - payload type is inferred from the method signature
    // - operation id is inferred from the method name
    // - message metadata is inferred from the payload type
    [Channel("orders.submitted", Servers = new[] { "inmemory" })]
    [SendOperation]
    public Task Publish(OrderSubmitted message)
    {
        return _publishEndpoint.Publish(message);
    }
}
