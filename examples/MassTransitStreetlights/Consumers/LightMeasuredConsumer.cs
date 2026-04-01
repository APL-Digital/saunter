using System.Threading.Tasks;
using MassTransit;
using MassTransitStreetlights.Contracts;
using Microsoft.Extensions.Logging;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitStreetlights.Consumers;

[AsyncApi]
public class LightMeasuredConsumer : IConsumer<LightMeasuredPayload>
{
    // Receive-side annotations live on the MassTransit consumer so the AsyncAPI
    // document reflects the bus boundary directly.
    private readonly ILogger<LightMeasuredConsumer> _logger;

    public LightMeasuredConsumer(ILogger<LightMeasuredConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Inform about environmental lighting conditions of a particular streetlight.
    /// </summary>
    [Channel("lightingMeasured", "smartylighting.streetlights.1.0.event.{streetlightId}.lighting.measured", Description = "The topic on which measured values may be produced and consumed.", Servers = new[] { "rabbitmq" })]
    [ChannelParameter("streetlightId", typeof(string), Description = "The ID of the streetlight.")]
    [ReceiveOperation(typeof(LightMeasuredPayload), OperationId = "receiveLightMeasurement", Summary = "Inform about environmental lighting conditions of a particular streetlight.")]
    [Message(typeof(LightMeasuredPayload), MessageId = "lightMeasured", Name = "lightMeasured", Title = "Light measured", Summary = "Inform about environmental lighting conditions of a particular streetlight.", HeadersType = typeof(CommonHeaders), ContentType = "application/json")]
    public Task Consume(ConsumeContext<LightMeasuredPayload> context)
    {
        _logger.LogInformation(
            "Received light measurement with lumens {Lumens} at {SentAt}",
            context.Message.Lumens,
            context.Message.SentAt);

        return Task.CompletedTask;
    }
}
