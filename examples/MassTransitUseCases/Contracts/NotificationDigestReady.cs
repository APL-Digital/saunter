namespace MassTransitUseCases.Contracts;

public class NotificationDigestReady
{
    public Guid CustomerId { get; set; }

    public int NotificationCount { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }
}
