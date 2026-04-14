namespace MassTransitUseCases.Contracts;

public class InventoryReservationRequested
{
    public Guid OrderId { get; set; }

    public string WarehouseId { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
