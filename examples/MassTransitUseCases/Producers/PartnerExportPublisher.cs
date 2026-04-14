using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.Contracts;

namespace MassTransitUseCases.Producers;

public class PartnerExportPublisher : IPartnerExportPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PartnerExportPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task Publish(PartnerExportRequested message)
    {
        return _publishEndpoint.Publish(message);
    }
}
