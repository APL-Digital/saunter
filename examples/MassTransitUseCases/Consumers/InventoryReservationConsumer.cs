using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Microsoft.Extensions.Logging;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Consumers;

[AsyncApi]
public class InventoryReservationConsumer : IConsumer<InventoryReservationRequested>
{
    private readonly ILogger<InventoryReservationConsumer> _logger;

    public InventoryReservationConsumer(ILogger<InventoryReservationConsumer> logger)
    {
        _logger = logger;
    }

    // Use case: a consumer that receives a command-like request and responds with a result message.
    [Channel(CommerceChannels.InventoryReservations, CommerceChannels.InventoryReservationsAddress, Servers = new[] { "rabbitmq" }, Summary = "Inventory reservation requests routed by warehouse.")]
    [ChannelParameter("warehouseId", typeof(string), Description = "Warehouse that should process the reservation.", DefaultValue = "primary", Examples = new[] { "primary", "overflow" })]
    [ChannelTag("inventory", Description = "Channels used to reserve stock and coordinate inventory workflows.", ExternalDocs = "https://example.com/docs/inventory", ExternalDocsDescription = "Inventory workflow documentation.")]
    [ReceiveOperation(typeof(InventoryReservationRequested), OperationId = "HandleInventoryReservation", Summary = "Handle a reservation request and send a reply.", Description = "Demonstrates receive-side request/reply modeling in AsyncAPI 3.", Reply = CommerceChannels.InventoryReservationsReply, ReplyMessagePayloadType = typeof(InventoryReserved), ReplyAddressLocation = "$message.header#/responseAddress")]
    [Message(typeof(InventoryReservationRequested), Name = "InventoryReservationRequested", Title = "Inventory reservation requested", Summary = "Request that a warehouse reserve inventory for an order.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation", ContentType = "application/json", ExternalDocs = "https://example.com/docs/inventory/reservations/request")]
    public async Task Consume(ConsumeContext<InventoryReservationRequested> context)
    {
        _logger.LogInformation("Reserving {Quantity}x {Sku} in warehouse {WarehouseId}", context.Message.Quantity, context.Message.Sku, context.Message.WarehouseId);

        await context.RespondAsync(new InventoryReserved
        {
            OrderId = context.Message.OrderId,
            ReservationId = $"reservation-{context.Message.OrderId:N}",
            WarehouseId = context.Message.WarehouseId,
            Sku = context.Message.Sku,
            Quantity = context.Message.Quantity,
        });
    }
}
