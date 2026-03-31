using MassTransitStreetlights.AsyncApi;
using MassTransitStreetlights.Consumers;
using MassTransitStreetlights.Producers;
using Microsoft.Extensions.DependencyInjection;
using Saunter;

namespace MassTransitStreetlights.Configuration;

public static class AsyncApiServiceCollectionExtensions
{
    public static IServiceCollection AddStreetlightsAsyncApi(this IServiceCollection services)
    {
        services.AddAsyncApiSchemaGeneration(options =>
        {
            options.AssemblyMarkerTypes = new[] { typeof(StreetlightCommandPublisher), typeof(LightMeasuredConsumer) };
            options.Middleware.UiTitle = "Streetlights RabbitMQ API";
            options.AsyncApi = StreetlightsAsyncApiDocument.Create();
        });

        return services;
    }
}
