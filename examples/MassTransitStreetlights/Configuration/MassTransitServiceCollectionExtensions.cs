using MassTransit;
using MassTransitStreetlights.Consumers;
using MassTransitStreetlights.Consumers.Definitions;
using MassTransitStreetlights.Producers;
using Microsoft.Extensions.DependencyInjection;

namespace MassTransitStreetlights.Configuration;

public static class MassTransitServiceCollectionExtensions
{
    public static IServiceCollection AddStreetlightsMessaging(this IServiceCollection services)
    {
        services.AddScoped<IStreetlightCommandPublisher, StreetlightCommandPublisher>();

        services.AddMassTransit(configurator =>
        {
            configurator.AddConsumer<LightMeasuredConsumer, LightMeasuredConsumerDefinition>();

            configurator.UsingInMemory((context, cfg) =>
            {
                // The example runs on MassTransit's in-memory transport so it works locally without
                // RabbitMQ. The generated AsyncAPI document still models the target RabbitMQ topology,
                // so the runtime transport and published spec are intentionally different here.
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
