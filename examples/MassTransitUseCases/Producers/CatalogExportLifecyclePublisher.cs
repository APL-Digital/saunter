using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class CatalogExportLifecyclePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public CatalogExportLifecyclePublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // Use case: one producer method can document several possible message variants on the same channel.
    [Channel(CommerceChannels.CatalogExportLifecycle, CommerceChannels.CatalogExportLifecycleAddress, Servers = new[] { "rabbitmq" }, Description = "Lifecycle notifications for catalog exports.")]
    [SendOperation(OperationId = "PublishCatalogExportLifecycle", Summary = "Publish catalog export lifecycle events.", Description = "Demonstrates multiple [Message] attributes on one producer method.")]
    [Message(typeof(CatalogExportStarted), Name = "CatalogExportStarted", Title = "Catalog export started", Summary = "A catalog export job started.")]
    [Message(typeof(CatalogExportCompleted), Name = "CatalogExportCompleted", Title = "Catalog export completed", Summary = "A catalog export job completed.")]
    public Task Publish(object message)
    {
        return _publishEndpoint.Publish(message);
    }
}
