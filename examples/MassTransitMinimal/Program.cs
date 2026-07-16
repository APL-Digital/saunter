using System;
using MassTransit;
using MassTransitMinimal.Consumers;
using MassTransitMinimal.Contracts;
using MassTransitMinimal.Producers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saunter;

const string baseAddress = "http://localhost:5001";

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(baseAddress);

// The entry assembly is scanned for [AsyncApi] types by default, the asyncapi version
// defaults to 3.0.0, and the UI title falls back to info.title.
builder.Services.AddAsyncApiSchemaGeneration(options =>
{
    options.AsyncApi = new AsyncApiDocumentDescriptor
    {
        Info = new AsyncApiInfoDescriptor
        {
            Title = "MassTransit Minimal",
            Version = "1.0.0",
            Description = "Minimal MassTransit + Saunter example that relies on annotation defaults and inference.",
        },
        Servers =
        {
            ["inmemory"] = new AsyncApiServerDescriptor
            {
                Host = "localhost",
                Protocol = "in-memory",
                Description = "MassTransit in-memory transport used by this sample.",
            }
        }
    };
});

builder.Services.AddScoped<OrderSubmittedPublisher>();
builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderSubmittedConsumer>();
    configurator.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
});

var app = builder.Build();

app.MapPost("/orders/{orderId:guid}", async (Guid orderId, OrderSubmittedPublisher publisher) =>
{
    await publisher.Publish(new OrderSubmitted
    {
        OrderId = orderId,
        SubmittedAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/orders/{orderId}");
});

app.MapAsyncApi();

// Print the AsyncAPI doc location
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var address in app.Urls)
    {
        logger.LogInformation("AsyncAPI doc available at: {URL}", $"{address}/asyncapi/asyncapi.json");
        logger.LogInformation("AsyncAPI UI available at: {URL}", $"{address}/asyncapi/ui/");
    }
});

app.Run();
