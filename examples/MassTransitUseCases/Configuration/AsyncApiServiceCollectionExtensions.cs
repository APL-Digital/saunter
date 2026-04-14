using MassTransitUseCases.AsyncApi;
using MassTransitUseCases.Consumers;
using MassTransitUseCases.Producers;
using Microsoft.Extensions.DependencyInjection;
using Saunter;

namespace MassTransitUseCases.Configuration;

public static class AsyncApiServiceCollectionExtensions
{
    public static IServiceCollection AddUseCaseAsyncApi(this IServiceCollection services)
    {
        services.AddAsyncApiSchemaGeneration(options =>
        {
            options.AssemblyMarkerTypes =
            [
                typeof(CatalogPriceChangedPublisher),
                typeof(CatalogPriceChangedConsumer),
                typeof(InventoryReservationRequester),
                typeof(InventoryReservationConsumer),
                typeof(BillingLifecyclePublisher),
                typeof(AccountingEventsConsumer),
                typeof(FulfillmentCommandSender),
                typeof(PickPackRequestedConsumer),
                typeof(SearchIndexSyncPublisher),
                typeof(CatalogExportLifecyclePublisher),
                typeof(PricingQuoteRequester),
                typeof(PricingQuoteConsumer),
                typeof(ICustomerPreferenceChangedConsumer),
                typeof(OrderProjectionProcessor),
                typeof(ComplianceDecisionPublisher),
                typeof(GeoInventoryAdjustedPublisher),
                typeof(NotificationDigestRequester),
                typeof(NotificationDigestConsumer),
                typeof(TenantCatalogPublisher),
                typeof(PartnerExportPublisher),
            ];
            options.Middleware.UiTitle = "MassTransit Use Cases";
            options.AsyncApi = CommerceAsyncApiDocument.Create();
        });

        return services;
    }
}
