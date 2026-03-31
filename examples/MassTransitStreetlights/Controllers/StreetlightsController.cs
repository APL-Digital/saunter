using System.Threading.Tasks;
using MassTransitStreetlights.Producers;
using Microsoft.AspNetCore.Mvc;

namespace MassTransitStreetlights.Controllers;

[ApiController]
[Route("api/streetlights")]
public class StreetlightsController : ControllerBase
{
    // The controller stays unannotated on purpose. It is only an HTTP adapter
    // that forwards commands to the message publisher.
    private readonly IStreetlightCommandPublisher _publisher;

    public StreetlightsController(IStreetlightCommandPublisher publisher)
    {
        _publisher = publisher;
    }

    /// <summary>
    /// Command a particular streetlight to turn the lights on.
    /// </summary>
    [HttpPost("{streetlightId}/turn-on")]
    public async Task<IActionResult> TurnOn(string streetlightId)
    {
        await _publisher.TurnOn(streetlightId);
        return Accepted(new { streetlightId, command = "on" });
    }

    /// <summary>
    /// Command a particular streetlight to turn the lights off.
    /// </summary>
    [HttpPost("{streetlightId}/turn-off")]
    public async Task<IActionResult> TurnOff(string streetlightId)
    {
        await _publisher.TurnOff(streetlightId);
        return Accepted(new { streetlightId, command = "off" });
    }

    /// <summary>
    /// Command a particular streetlight to dim the lights.
    /// </summary>
    [HttpPost("{streetlightId}/dim")]
    public async Task<IActionResult> Dim(string streetlightId, [FromBody] DimLightRequest request)
    {
        await _publisher.Dim(streetlightId, request.Percentage);
        return Accepted(new { streetlightId, percentage = request.Percentage });
    }
}
