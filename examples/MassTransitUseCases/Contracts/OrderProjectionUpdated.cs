namespace MassTransitUseCases.Contracts;

public class OrderProjectionUpdated
{
    public Guid OrderId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
