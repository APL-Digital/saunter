using MassTransit;
using MassTransitUseCases.Consumers;
using MassTransitUseCases.Contracts;
using MassTransitUseCases.Producers;
using Microsoft.Extensions.DependencyInjection;

namespace MassTransitUseCases.Configuration;

public static class MassTransitServiceCollectionExtensions
{
    public static IServiceCollection AddUseCaseMessaging(this IServiceCollection services)
    {
        services.AddScoped<CatalogPriceChangedPublisher>();
        services.AddScoped<InventoryReservationRequester>();
        services.AddScoped<BillingLifecyclePublisher>();
        services.AddScoped<FulfillmentCommandSender>();
        services.AddScoped<SearchIndexSyncPublisher>();
        services.AddScoped<CatalogExportLifecyclePublisher>();
        services.AddScoped<PricingQuoteRequester>();
        services.AddScoped<GeoInventoryAdjustedPublisher>();
        services.AddScoped<NotificationDigestRequester>();
        services.AddScoped<TenantCatalogPublisher>();
        services.AddScoped<IPartnerExportPublisher, PartnerExportPublisher>();

        services.AddMassTransit(configurator =>
        {
            configurator.AddConsumer<CatalogPriceChangedConsumer>();
            configurator.AddConsumer<InventoryReservationConsumer>();
            configurator.AddConsumer<AccountingEventsConsumer>();
            configurator.AddConsumer<PickPackRequestedConsumer>();
            configurator.AddConsumer<PricingQuoteConsumer>();
            configurator.AddConsumer<CustomerPreferenceChangedConsumer>();
            configurator.AddConsumer<OrderProjectionProcessor>();
            configurator.AddConsumer<NotificationDigestConsumer>();
            configurator.AddRequestClient<InventoryReservationRequested>();
            configurator.AddRequestClient<PricingQuoteRequested>();
            configurator.AddRequestClient<NotificationDigestRequested>();

            configurator.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
