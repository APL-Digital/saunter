namespace MassTransitUseCases.Contracts;

public class CatalogExportStarted
{
    public Guid ExportId { get; set; }

    public string Market { get; set; } = string.Empty;

    public DateTimeOffset StartedAt { get; set; }
}
