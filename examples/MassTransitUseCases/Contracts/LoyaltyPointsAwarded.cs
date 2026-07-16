using System;

namespace MassTransitUseCases.Contracts;

public class LoyaltyPointsAwarded
{
    public Guid CustomerId { get; set; }

    public int Points { get; set; }

    public DateTimeOffset AwardedAt { get; set; }
}
