using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Microsoft.Extensions.Logging;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Consumers;

[AsyncApi]
[Channel(CommerceChannels.AccountingEvents, CommerceChannels.AccountingEventsAddress, Servers = new[] { "rabbitmq" }, Description = "Accounting-side events that are consumed as one logical boundary.")]
[ChannelTag("accounting", Description = "Channels used by accounting and reconciliation workflows.")]
[ReceiveOperation(OperationId = "ConsumeAccountingEvents", Summary = "Consume multiple accounting event types from one boundary.", Description = "Demonstrates class-level receive annotations with more than one MassTransit consumer contract.")]
public class AccountingEventsConsumer :
    IConsumer<PaymentCaptured>,
    IConsumer<RefundIssued>
{
    private readonly ILogger<AccountingEventsConsumer> _logger;

    public AccountingEventsConsumer(ILogger<AccountingEventsConsumer> logger)
    {
        _logger = logger;
    }

    // Use case: one accounting boundary receives captured payments.
    [Message(typeof(PaymentCaptured), Name = "PaymentCaptured", Title = "Payment captured", Summary = "A payment was successfully captured.")]
    public Task Consume(ConsumeContext<PaymentCaptured> context)
    {
        _logger.LogInformation("Captured payment {PaymentId} for invoice {InvoiceNumber}", context.Message.PaymentId, context.Message.InvoiceNumber);
        return Task.CompletedTask;
    }

    // Use case: the same accounting boundary also receives refunds on the same channel.
    [Message(typeof(RefundIssued), Name = "RefundIssued", Title = "Refund issued", Summary = "A refund was issued back to the customer.")]
    public Task Consume(ConsumeContext<RefundIssued> context)
    {
        _logger.LogInformation("Issued refund {RefundId} for invoice {InvoiceNumber}", context.Message.RefundId, context.Message.InvoiceNumber);
        return Task.CompletedTask;
    }
}
