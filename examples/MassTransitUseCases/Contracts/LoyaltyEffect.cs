using System.Text.Json.Serialization;

namespace MassTransitUseCases.Contracts;

// Use case: a closed effect hierarchy keeps its discriminator and subtype fields
// in the generated AsyncAPI payload, including when nested in a collection.
[JsonPolymorphic]
[JsonDerivedType(typeof(LoyaltyTierEffect), "loyaltyTier")]
public abstract class LoyaltyEffect
{
    public string BackendId { get; init; } = "example";
}
