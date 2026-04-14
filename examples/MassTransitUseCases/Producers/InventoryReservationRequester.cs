using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class InventoryReservationRequester
{
    private readonly IRequestClient<InventoryReservationRequested> _requestClient;

    public InventoryReservationRequester(IRequestClient<InventoryReservationRequested> requestClient)
    {
        _requestClient = requestClient;
    }

    // Use case: request/reply over MassTransit with rich AsyncAPI metadata on the request channel.
    // This shows channel parameters, channel tags, headers, correlation ids, and a documented reply channel.
    [Channel(CommerceChannels.InventoryReservations, CommerceChannels.InventoryReservationsAddress, Servers = new[] { "rabbitmq" }, Summary = "Inventory reservation requests routed by warehouse.")]
    [ChannelParameter("warehouseId", typeof(string), Description = "Warehouse that should process the reservation.", DefaultValue = "primary", Examples = new[] { "primary", "overflow" })]
    [ChannelTag("inventory", Description = "Channels used to reserve stock and coordinate inventory workflows.", ExternalDocs = "https://example.com/docs/inventory", ExternalDocsDescription = "Inventory workflow documentation.")]
    [SendOperation(typeof(InventoryReservationRequested), OperationId = "RequestInventoryReservation", Summary = "Send a reservation request and wait for a reply.", Description = "Demonstrates request/reply, explicit operation metadata, and a parameterized channel address.", Reply = CommerceChannels.InventoryReservationsReply, ReplyMessagePayloadType = typeof(InventoryReserved), ReplyAddressLocation = "$message.header#/responseAddress")]
    [Message(typeof(InventoryReservationRequested), Name = "InventoryReservationRequested", Title = "Inventory reservation requested", Summary = "Request that a warehouse reserve inventory for an order.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation", ContentType = "application/json", ExternalDocs = "https://example.com/docs/inventory/reservations/request")]
    public async Task<InventoryReserved> Request(string warehouseId, InventoryReservationRequested message)
    {
        message.WarehouseId = warehouseId;

        var response = await _requestClient.GetResponse<InventoryReserved>(message);
        return response.Message;
    }
}
