using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class GeoInventoryAdjustedPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public GeoInventoryAdjustedPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // Use case: simple string-based tags on the channel, operation, and message, plus explicit parameter location metadata.
    [Channel(CommerceChannels.GeoInventoryAdjustedAddress, ChannelId = CommerceChannels.GeoInventoryAdjusted, Servers = new[] { "rabbitmq" }, Title = "Geo inventory adjusted", Summary = "Inventory adjustments partitioned by region.", Description = "Demonstrates the simpler string-array tag surface on ChannelAttribute.", Tags = new[] { "inventory", "geo" })]
    [ChannelParameter("region", typeof(string), Description = "Sales region carried in the channel address and echoed in headers.", Location = "$message.header#/region", Examples = new[] { "eu-west", "us-east" })]
    [SendOperation(typeof(InventoryAdjusted), "inventory", "projection", OperationId = "PublishGeoInventoryAdjusted", Summary = "Publish a regional inventory adjustment.")]
    [Message(typeof(InventoryAdjusted), "inventory", "adjustment", Name = "InventoryAdjusted", Title = "Inventory adjusted", Summary = "Inventory changed in a particular region.", ExternalDocs = "https://example.com/docs/inventory-adjustments", ExternalDocsDescription = "Inventory adjustment event semantics.")]
    public Task Publish(string region, InventoryAdjusted message)
    {
        message.Region = region;
        return _publishEndpoint.Publish(message);
    }
}
