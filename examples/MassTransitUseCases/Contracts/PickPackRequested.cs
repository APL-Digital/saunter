namespace MassTransitUseCases.Contracts;

public class PickPackRequested
{
    public Guid OrderId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
