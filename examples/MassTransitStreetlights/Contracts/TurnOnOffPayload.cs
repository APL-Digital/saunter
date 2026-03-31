using System;
using System.Runtime.Serialization;

namespace MassTransitStreetlights.Contracts;

public class TurnOnOffPayload
{
    public TurnOnOffCommand Command { get; set; }

    public DateTime SentAt { get; set; }
}

public enum TurnOnOffCommand
{
    [EnumMember(Value = "on")]
    On,
    [EnumMember(Value = "off")]
    Off
}
