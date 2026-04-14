using System.Threading.Tasks;
using MassTransit;
using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Contracts;
using Microsoft.Extensions.Logging;
using Saunter.AttributeProvider.Attributes;

namespace MassTransitUseCases.Consumers;

[AsyncApi]
public class CatalogPriceChangedConsumer : IConsumer<ProductPriceChanged>
{
    private readonly ILogger<CatalogPriceChangedConsumer> _logger;

    public CatalogPriceChangedConsumer(ILogger<CatalogPriceChangedConsumer> logger)
    {
        _logger = logger;
    }

    // Use case: the matching happy-path receive side for a plain published event.
    [Channel(CommerceChannels.CatalogPriceChangedAddress, Servers = new[] { "inmemory" })]
    [ReceiveOperation]
    public Task Consume(ConsumeContext<ProductPriceChanged> context)
    {
        _logger.LogInformation("Observed price change for {Sku}: {OldPrice} -> {NewPrice}", context.Message.Sku, context.Message.OldPrice, context.Message.NewPrice);
        return Task.CompletedTask;
    }
}
