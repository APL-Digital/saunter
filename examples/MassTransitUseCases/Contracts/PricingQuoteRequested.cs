namespace MassTransitUseCases.Contracts;

public class PricingQuoteRequested
{
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
}
