using System;

namespace MassTransitStreetlights.Contracts;

public class LightMeasuredPayload
{
    // Gap: the target YAML constrains this with minimum: 0.
    // Saunter's schema generator does not currently emit numeric bounds.
    public int Lumens { get; set; }

    public DateTime SentAt { get; set; }
}
