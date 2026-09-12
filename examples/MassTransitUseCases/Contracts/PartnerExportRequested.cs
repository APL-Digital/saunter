using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MassTransitUseCases.Contracts;

public class PartnerExportRequested
{
    [StringLength(64, MinimumLength = 1)]
    [Description("Partner identifier. The sender validates this bound before publishing.")]
    public string PartnerId { get; set; } = string.Empty;

    public string ExportType { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; }
}
