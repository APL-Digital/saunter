namespace MassTransitUseCases.Contracts;

public class TenantCatalogRebuilt
{
    public string TenantId { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public DateTimeOffset RebuiltAt { get; set; }
}
