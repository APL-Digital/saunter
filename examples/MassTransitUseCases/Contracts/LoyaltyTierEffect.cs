namespace MassTransitUseCases.Contracts;

public sealed class LoyaltyTierEffect : LoyaltyEffect
{
    public string TierName { get; init; } = "Gold";
}
