using Saunter.AttributeProvider.Attributes;

namespace MultiDocument.Fleet;

public record VehiclePositionChanged(string VehicleId, double Latitude, double Longitude);

// Both publishers in this sample share the same attribute document name ("v1").
// The hosted documents are split per publisher through TypeFilter in Program.cs.
[AsyncApi("v1")]
public class FleetPublisher
{
    [Channel("fleet.vehicle.position")]
    [SendOperation]
    public void PublishVehiclePosition(VehiclePositionChanged position) { }
}
