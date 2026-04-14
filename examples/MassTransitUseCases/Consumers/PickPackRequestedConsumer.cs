using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Microsoft.Extensions.Logging;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Consumers;

[AsyncApi]
public class PickPackRequestedConsumer : IConsumer<PickPackRequested>
{
    private readonly ILogger<PickPackRequestedConsumer> _logger;

    public PickPackRequestedConsumer(ILogger<PickPackRequestedConsumer> logger)
    {
        _logger = logger;
    }

    // Use case: the direct-queue consumer counterpart to a send endpoint producer.
    [Channel(CommerceChannels.FulfillmentPickPack, CommerceChannels.FulfillmentPickPackAddress, Servers = new[] { "rabbitmq" }, Description = "Direct command queue for warehouse pick/pack work.")]
    [ReceiveOperation(typeof(PickPackRequested), OperationId = "HandlePickPackRequested", Summary = "Handle a pick/pack command.")]
    [Message(typeof(PickPackRequested), Name = "PickPackRequested", Title = "Pick pack requested", Summary = "Ask fulfillment to pick and pack inventory for an order.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation")]
    public Task Consume(ConsumeContext<PickPackRequested> context)
    {
        _logger.LogInformation("Picking order {OrderId} for sku {Sku}", context.Message.OrderId, context.Message.Sku);
        return Task.CompletedTask;
    }
}
