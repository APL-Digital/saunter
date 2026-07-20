namespace MassTransitUseCases.Contracts;

public class InventoryReservationRejected
{
    public Guid OrderId { get; set; }

    public string WarehouseId { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
