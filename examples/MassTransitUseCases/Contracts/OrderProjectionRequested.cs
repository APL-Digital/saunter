namespace MassTransitUseCases.Contracts;

public class OrderProjectionRequested
{
    public Guid OrderId { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
}
