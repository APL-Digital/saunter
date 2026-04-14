namespace MassTransitUseCases.Contracts;

public class SearchIndexSyncRequested
{
    public string IndexName { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; }
}
