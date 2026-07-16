using System.Threading.Tasks;
using MassTransit;
using MassTransitMinimal.Contracts;
using Microsoft.Extensions.Logging;

namespace MassTransitMinimal.Consumers;

// No AsyncAPI attributes: this consumer is documented by MassTransit consumer discovery
// (options.Discovery.DiscoverMassTransitConsumers = true in Program.cs). The channel
// address defaults to the message type's Namespace:TypeName (MassTransit MessageUrn form)
// and the operation id to {ConsumerName}.{MessageName}.receive.
public class OrderArchivedConsumer : IConsumer<OrderArchived>
{
    private readonly ILogger<OrderArchivedConsumer> _logger;

    public OrderArchivedConsumer(ILogger<OrderArchivedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderArchived> context)
    {
        _logger.LogInformation("Archived order {OrderId}", context.Message.OrderId);
        return Task.CompletedTask;
    }
}
