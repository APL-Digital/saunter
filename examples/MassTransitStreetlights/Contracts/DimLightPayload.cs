using System;

namespace MassTransitStreetlights.Contracts;

public class DimLightPayload
{
    public string StreetlightId { get; set; } = string.Empty;

    // Gap: the target YAML constrains this with minimum: 0 and maximum: 100.
    // Saunter does not currently emit those schema keywords from CLR models.
    public int Percentage { get; set; }

    public DateTime SentAt { get; set; }
}
