namespace MassTransitUseCases.Contracts;

public class NotificationDigestRequested
{
    public Guid CustomerId { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
}
