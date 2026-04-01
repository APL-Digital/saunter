using System;

namespace MassTransitMinimal.Contracts;

public class OrderSubmitted
{
    public Guid OrderId { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
}
