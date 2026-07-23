using System.Text.Json.Serialization;

namespace MassTransitUseCases.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<InventoryReservationUrgency>))]
public enum InventoryReservationUrgency
{
    [JsonStringEnumMemberName("standard")]
    Standard,

    [JsonStringEnumMemberName("expedited")]
    Expedited
}
