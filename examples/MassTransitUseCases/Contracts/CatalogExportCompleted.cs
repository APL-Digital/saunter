namespace MassTransitUseCases.Contracts;

public class CatalogExportCompleted
{
    public Guid ExportId { get; set; }

    public string Market { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    public DateTimeOffset CompletedAt { get; set; }
}
