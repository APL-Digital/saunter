using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Consumers;

[AsyncApi]
public class PricingQuoteConsumer : IConsumer<PricingQuoteRequested>
{
    // Use case: the receive side of a fixed-reply-address interaction.
    [Channel(CommerceChannels.PricingQuoteRequests, CommerceChannels.PricingQuoteRequestsAddress, Servers = new[] { "rabbitmq" }, Description = "Pricing quote requests submitted by commerce frontends.")]
    [ReceiveOperation(typeof(PricingQuoteRequested), OperationId = "HandlePricingQuoteRequested", Summary = "Handle a quote request and return pricing.", Reply = CommerceChannels.PricingQuoteReplies, ReplyChannelAddress = "pricing/quotes/replies", ReplyMessagePayloadType = typeof(PricingQuoteReady), Description = "Shows the fixed reply-address variant of AsyncAPI request/reply modeling.")]
    [Message(typeof(PricingQuoteRequested), Name = "PricingQuoteRequested", Title = "Pricing quote requested", Summary = "Ask pricing for a quote on a SKU and quantity.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation")]
    public async Task Consume(ConsumeContext<PricingQuoteRequested> context)
    {
        var unitPrice = 12.50m;

        await context.RespondAsync(new PricingQuoteReady
        {
            Sku = context.Message.Sku,
            Quantity = context.Message.Quantity,
            UnitPrice = unitPrice,
            TotalPrice = unitPrice * context.Message.Quantity,
        });
    }
}
