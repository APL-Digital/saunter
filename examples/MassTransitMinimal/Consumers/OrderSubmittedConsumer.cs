using MassTransit;
using MassTransitMinimal.Contracts;
using Microsoft.Extensions.Logging;
using Saunter.AttributeProvider.Attributes;
using System.Threading.Tasks;

namespace MassTransitMinimal.Consumers;

[AsyncApi]
public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    private readonly ILogger<OrderSubmittedConsumer> _logger;

    public OrderSubmittedConsumer(ILogger<OrderSubmittedConsumer> logger)
    {
        _logger = logger;
    }

    // Minimal receive-side path:
    // - channel id is inferred from the address
    // - payload type is inferred from ConsumeContext<T>
    // - operation id is inferred from the method name
    [Channel("orders.submitted", Servers = new[] { "inmemory" })]
    [ReceiveOperation]
    public Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        _logger.LogInformation("Received order {OrderId}", context.Message.OrderId);
        return Task.CompletedTask;
    }
}
