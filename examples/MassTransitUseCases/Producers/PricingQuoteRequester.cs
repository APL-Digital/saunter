using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Producers;

[AsyncApi]
public class PricingQuoteRequester
{
    private readonly IRequestClient<PricingQuoteRequested> _requestClient;

    public PricingQuoteRequester(IRequestClient<PricingQuoteRequested> requestClient)
    {
        _requestClient = requestClient;
    }

    // Use case: request/reply with a statically documented reply channel address.
    [Channel(CommerceChannels.PricingQuoteRequests, CommerceChannels.PricingQuoteRequestsAddress, Servers = new[] { "rabbitmq" }, Description = "Pricing quote requests submitted by commerce frontends.")]
    [SendOperation(typeof(PricingQuoteRequested), OperationId = "RequestPricingQuote", Summary = "Request a price quote.", Reply = CommerceChannels.PricingQuoteReplies, ReplyChannelAddress = "pricing/quotes/replies", ReplyMessagePayloadType = typeof(PricingQuoteReady), Description = "Shows the fixed reply-address variant of AsyncAPI request/reply modeling.")]
    [Message(typeof(PricingQuoteRequested), Name = "PricingQuoteRequested", Title = "Pricing quote requested", Summary = "Ask pricing for a quote on a SKU and quantity.", HeadersType = typeof(CommerceMessageHeaders), CorrelationId = "workflowCorrelation")]
    public async Task<PricingQuoteReady> Request(PricingQuoteRequested message)
    {
        var response = await _requestClient.GetResponse<PricingQuoteReady>(message);
        return response.Message;
    }
}
