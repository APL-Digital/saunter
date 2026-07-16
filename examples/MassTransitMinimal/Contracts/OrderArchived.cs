using System;

namespace MassTransitMinimal.Contracts;

public class OrderArchived
{
    public Guid OrderId { get; set; }

    public DateTimeOffset ArchivedAt { get; set; }
}
