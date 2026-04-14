using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class CatalogPriceChangedPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public CatalogPriceChangedPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // Use case: the smallest useful MassTransit producer boundary.
    // It documents a plain domain event where Saunter can infer the operation id and payload metadata.
    [Channel(CommerceChannels.CatalogPriceChangedAddress, Servers = new[] { "inmemory" })]
    [SendOperation]
    public Task Publish(ProductPriceChanged message)
    {
        return _publishEndpoint.Publish(message);
    }
}
