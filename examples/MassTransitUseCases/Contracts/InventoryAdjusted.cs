namespace MassTransitUseCases.Contracts;

public class InventoryAdjusted
{
    public string Region { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int Delta { get; set; }

    public DateTimeOffset AdjustedAt { get; set; }
}
