using System.Threading.Tasks;

namespace MassTransitStreetlights.Producers;

public interface IStreetlightCommandPublisher
{
    Task TurnOn(string streetlightId);

    Task TurnOff(string streetlightId);

    Task Dim(string streetlightId, int percentage);
}
