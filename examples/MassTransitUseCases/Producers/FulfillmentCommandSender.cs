using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class FulfillmentCommandSender
{
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public FulfillmentCommandSender(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    // Use case: send a command directly to a queue instead of publishing an event to whoever is listening.
    [Channel(CommerceChannels.FulfillmentPickPack, CommerceChannels.FulfillmentPickPackAddress, Servers = new[] { "rabbitmq" }, Description = "Direct command queue for warehouse pick/pack work.")]
    [SendOperation(typeof(PickPackRequested), OperationId = "SendPickPackRequested", Summary = "Send a pick/pack command to fulfillment.", Description = "Demonstrates a MassTransit send-to-endpoint boundary rather than publish.")]
    [Message(typeof(PickPackRequested), Name = "PickPackRequested", Title = "Pick pack requested", Summary = "Ask fulfillment to pick and pack inventory for an order.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation")]
    public async Task Send(PickPackRequested message)
    {
        var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:fulfillment-pick-pack"));
        await endpoint.Send(message);
    }
}
