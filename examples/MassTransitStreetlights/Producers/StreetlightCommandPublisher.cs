using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransitStreetlights.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitStreetlights.Producers;

[AsyncApi]
public class StreetlightCommandPublisher : IStreetlightCommandPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public StreetlightCommandPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Command a particular streetlight to turn the lights on.
    /// </summary>
    // Gap: the target YAML aliases the channel message as `turnOn` while reusing the same
    // underlying message component shape as `turnOff`. Saunter currently treats message ids
    // as distinct component messages, so the aliasing/reuse pattern cannot be expressed directly.
    [Channel("lightTurnOn", "smartylighting.streetlights.1.0.action.{streetlightId}.turn.on", Description = "Command channel for turning a streetlight on.", Servers = new[] { "rabbitmq" })]
    [ChannelParameter("streetlightId", typeof(string), Description = "The ID of the streetlight.")]
    [SendOperation(typeof(TurnOnOffPayload), OperationId = "turnOn")]
    [Message(typeof(TurnOnOffPayload), MessageId = "turnOn", Name = "turnOn", Title = "Turn on", Summary = "Command a particular streetlight to turn the lights on.", HeadersType = typeof(CommonHeaders))]
    public Task TurnOn(string streetlightId)
    {
        return _publishEndpoint.Publish(new TurnOnOffPayload
        {
            Command = TurnOnOffCommand.On,
            SentAt = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Command a particular streetlight to turn the lights off.
    /// </summary>
    // Same gap as above: `turnOff` is modeled as its own message id instead of a second alias
    // to a shared component message.
    [Channel("lightTurnOff", "smartylighting.streetlights.1.0.action.{streetlightId}.turn.off", Description = "Command channel for turning a streetlight off.", Servers = new[] { "rabbitmq" })]
    [ChannelParameter("streetlightId", typeof(string), Description = "The ID of the streetlight.")]
    [SendOperation(typeof(TurnOnOffPayload), OperationId = "turnOff")]
    [Message(typeof(TurnOnOffPayload), MessageId = "turnOff", Name = "turnOff", Title = "Turn off", Summary = "Command a particular streetlight to turn the lights off.", HeadersType = typeof(CommonHeaders))]
    public Task TurnOff(string streetlightId)
    {
        return _publishEndpoint.Publish(new TurnOnOffPayload
        {
            Command = TurnOnOffCommand.Off,
            SentAt = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Command a particular streetlight to dim the lights.
    /// </summary>
    [Channel("lightsDim", "smartylighting.streetlights.1.0.action.{streetlightId}.dim", Description = "Command channel for dimming a streetlight.", Servers = new[] { "rabbitmq" })]
    [ChannelParameter("streetlightId", typeof(string), Description = "The ID of the streetlight.")]
    [SendOperation(typeof(DimLightPayload), OperationId = "dimLight")]
    [Message(typeof(DimLightPayload), MessageId = "dimLight", Name = "dimLight", Title = "Dim light", Summary = "Command a particular streetlight to dim the lights.", HeadersType = typeof(CommonHeaders), ContentType = "application/json")]
    public Task Dim(string streetlightId, int percentage)
    {
        return _publishEndpoint.Publish(new DimLightPayload
        {
            Percentage = percentage,
            SentAt = DateTime.UtcNow,
        });
    }
}
