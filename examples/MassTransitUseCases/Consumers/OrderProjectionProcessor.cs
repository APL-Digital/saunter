using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Consumers;

[AsyncApi]
public class OrderProjectionProcessor : IConsumer<OrderProjectionRequested>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderProjectionProcessor(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // Use case: model a processor that receives one message and emits another from the same boundary method.
    // This demonstrates a single method carrying both ReceiveOperation and SendOperation annotations.
    [Channel(CommerceChannels.OrderProjectionPipeline, CommerceChannels.OrderProjectionPipelineAddress, Servers = new[] { "rabbitmq" }, Description = "Pipeline step that refreshes read-model projections for orders.")]
    [ReceiveOperation(typeof(OrderProjectionRequested), OperationId = "HandleOrderProjectionRequested", Summary = "Receive an order projection refresh request.")]
    [SendOperation(typeof(OrderProjectionUpdated), OperationId = "PublishOrderProjectionUpdated", Summary = "Emit an order projection updated event.")]
    public async Task Consume(ConsumeContext<OrderProjectionRequested> context)
    {
        await _publishEndpoint.Publish(new OrderProjectionUpdated
        {
            OrderId = context.Message.OrderId,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
    }
}
