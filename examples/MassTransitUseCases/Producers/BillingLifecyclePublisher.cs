using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
[Channel(CommerceChannels.BillingLifecycle, CommerceChannels.BillingLifecycleAddress, Servers = new[] { "rabbitmq" }, Description = "Billing lifecycle events emitted by the billing domain.")]
[ChannelTag("billing", Description = "Channels that represent billing and invoice workflows.")]
[SendOperation(OperationId = "PublishBillingLifecycleEvents", Summary = "Publish multiple billing lifecycle event types from one producer boundary.", Description = "Demonstrates class-level annotations that aggregate multiple message contracts into a single AsyncAPI operation.")]
public class BillingLifecyclePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public BillingLifecyclePublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    // Use case: one publisher service emits several related event types on the same channel.
    [Message(typeof(InvoiceIssued), Name = "InvoiceIssued", Title = "Invoice issued", Summary = "An invoice was issued to a customer.")]
    public Task PublishInvoiceIssued(InvoiceIssued message)
    {
        return _publishEndpoint.Publish(message);
    }

    // Use case: same channel and operation surface, different event variant.
    [Message(typeof(InvoicePaid), Name = "InvoicePaid", Title = "Invoice paid", Summary = "An invoice was paid in full.")]
    public Task PublishInvoicePaid(InvoicePaid message)
    {
        return _publishEndpoint.Publish(message);
    }
}
