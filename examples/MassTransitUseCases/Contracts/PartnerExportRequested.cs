namespace MassTransitUseCases.Contracts;

public class PartnerExportRequested
{
    public string PartnerId { get; set; } = string.Empty;

    public string ExportType { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; }
}
