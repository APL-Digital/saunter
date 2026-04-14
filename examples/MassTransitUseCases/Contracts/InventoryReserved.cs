namespace MassTransitUseCases.Contracts;

public class InventoryReserved
{
    public Guid OrderId { get; set; }

    public string ReservationId { get; set; } = string.Empty;

    public string WarehouseId { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
