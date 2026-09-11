using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.Contracts;
using Microsoft.Extensions.Logging;

namespace MassTransitUseCases.Consumers;

// Use case: convention-based MassTransit consumer discovery. This consumer carries no
// AsyncAPI attributes; it is documented because Discovery.DiscoverMassTransitConsumers is
// enabled in Configuration/AsyncApiServiceCollectionExtensions.cs. The channel address
// defaults to the message type's Namespace:TypeName (MassTransit MessageUrn form) and the
// operation id to {ConsumerName}.{MessageName}.receive. Annotated consumers are never
// double-documented: discovery skips any type that carries AsyncAPI attributes.
public class LoyaltyPointsAwardedConsumer : IConsumer<LoyaltyPointsAwarded>
{
    private readonly ILogger<LoyaltyPointsAwardedConsumer> _logger;

    public LoyaltyPointsAwardedConsumer(ILogger<LoyaltyPointsAwardedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<LoyaltyPointsAwarded> context)
    {
        _logger.LogInformation("Awarded {Points} loyalty points to {CustomerId}", context.Message.Points, context.Message.CustomerId);
        foreach (var effect in context.Message.Effects)
        {
            if (effect is LoyaltyTierEffect tier)
            {
                _logger.LogInformation("Customer {CustomerId} has tier {TierName} from {BackendId}",
                    context.Message.CustomerId, tier.TierName, tier.BackendId);
            }
        }

        return Task.CompletedTask;
    }
}
