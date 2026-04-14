using System.Threading.Tasks;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public interface IPartnerExportPublisher
{
    // Use case: keep the AsyncAPI annotations on an interface when the implementation is intentionally thin.
    [Channel(CommerceChannels.PartnerExportRequestedAddress, ChannelId = CommerceChannels.PartnerExportRequested, Servers = new[] { "rabbitmq" }, Description = "Export requests sent to partner integrations.")]
    [ChannelTag("integration", Description = "Channels used when integrating with external partners.", ExternalDocs = "https://example.com/docs/integrations", ExternalDocsDescription = "Partner integration documentation.")]
    [SendOperation(typeof(PartnerExportRequested), OperationId = "PublishPartnerExportRequested", Summary = "Publish a partner export request.", Description = "Demonstrates interface-based annotation discovery and named ChannelId overrides.")]
    [Message(typeof(PartnerExportRequested), Name = "PartnerExportRequested", Title = "Partner export requested", Summary = "Request a partner-specific export to be generated.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation", ExternalDocs = "https://example.com/docs/partner-exports")]
    Task Publish(PartnerExportRequested message);
}
