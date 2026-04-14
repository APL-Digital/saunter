using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using MassTransitUseCases.Resolvers;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class TenantCatalogPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public TenantCatalogPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // Use case: generate the channel address from a custom resolver instead of hardcoding it in the attribute.
    [Channel(CommerceChannels.TenantCatalogRebuilt, typeof(TenantCatalogChannelResolver), typeof(TenantCatalogRebuilt), Servers = new[] { "rabbitmq" }, Description = "Channel address resolved dynamically from the payload type.")]
    [SendOperation(typeof(TenantCatalogRebuilt), OperationId = "PublishTenantCatalogRebuilt", Summary = "Publish a tenant-scoped catalog rebuild event.", Description = "Demonstrates Saunter's custom IChannelResolver integration.")]
    public Task Publish(TenantCatalogRebuilt message)
    {
        return _publishEndpoint.Publish(message);
    }
}
