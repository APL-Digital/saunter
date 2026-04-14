namespace MassTransitUseCases.Contracts;

public class ProductPriceChanged
{
    public string Sku { get; set; } = string.Empty;

    public decimal OldPrice { get; set; }

    public decimal NewPrice { get; set; }

    public DateTimeOffset ChangedAt { get; set; }
}
