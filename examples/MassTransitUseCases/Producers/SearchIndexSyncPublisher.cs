using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class SearchIndexSyncPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public SearchIndexSyncPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // Use case: attach reusable AsyncAPI bindings for the channel, operation, and message.
    [Channel(CommerceChannels.SearchIndexSync, CommerceChannels.SearchIndexSyncAddress, Servers = new[] { "rabbitmq" }, BindingsRef = "searchIndexKafkaTopic", Description = "Requests to rebuild or refresh search indexes.")]
    [ChannelParameter("indexName", typeof(string), Description = "Logical search index name.", Examples = new[] { "products", "categories" })]
    [SendOperation(typeof(SearchIndexSyncRequested), OperationId = "PublishSearchIndexSyncRequested", Summary = "Publish a search index sync request.", BindingsRef = "searchIndexKafkaProducer", Description = "Demonstrates binding references on all three AsyncAPI levels.")]
    [Message(typeof(SearchIndexSyncRequested), Name = "SearchIndexSyncRequested", Title = "Search index sync requested", Summary = "Request the search platform to rebuild an index.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation", BindingsRef = "searchIndexKafkaMessage")]
    public Task Publish(SearchIndexSyncRequested message)
    {
        return _publishEndpoint.Publish(message);
    }
}
