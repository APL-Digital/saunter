using System;
using MassTransitUseCases.Configuration;
using MassTransitUseCases.Contracts;
using MassTransitUseCases.Producers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saunter;

const string baseAddress = "http://localhost:5002";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(console => console.SingleLine = true);
builder.WebHost.UseUrls(baseAddress);

builder.Services.AddUseCaseAsyncApi();
builder.Services.AddUseCaseMessaging();

var app = builder.Build();

app.MapPost("/catalog/prices/{sku}", async (string sku, decimal oldPrice, decimal newPrice, CatalogPriceChangedPublisher publisher) =>
{
    await publisher.Publish(new ProductPriceChanged
    {
        Sku = sku,
        OldPrice = oldPrice,
        NewPrice = newPrice,
        ChangedAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/catalog/prices/{sku}");
});

app.MapPost("/inventory/warehouses/{warehouseId}/reservations", async (string warehouseId, Guid orderId, string sku, int quantity, InventoryReservationRequester requester) =>
{
    var response = await requester.Request(
        warehouseId,
        new InventoryReservationRequested
        {
            OrderId = orderId,
            WarehouseId = warehouseId,
            Sku = sku,
            Quantity = quantity,
        });

    return Results.Accepted($"/inventory/warehouses/{warehouseId}/reservations/{response.ReservationId}", response);
});

app.MapPost("/billing/invoices/{invoiceNumber}/issued", async (string invoiceNumber, decimal amount, BillingLifecyclePublisher publisher) =>
{
    await publisher.PublishInvoiceIssued(new InvoiceIssued
    {
        InvoiceNumber = invoiceNumber,
        Amount = amount,
        Currency = "EUR",
        IssuedAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/billing/invoices/{invoiceNumber}");
});

app.MapPost("/billing/invoices/{invoiceNumber}/paid", async (string invoiceNumber, decimal amount, BillingLifecyclePublisher publisher) =>
{
    await publisher.PublishInvoicePaid(new InvoicePaid
    {
        InvoiceNumber = invoiceNumber,
        Amount = amount,
        Currency = "EUR",
        PaidAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/billing/invoices/{invoiceNumber}/payments");
});

app.MapPost("/fulfillment/orders/{orderId:guid}/pick-pack", async (Guid orderId, string sku, int quantity, FulfillmentCommandSender sender) =>
{
    await sender.Send(new PickPackRequested
    {
        OrderId = orderId,
        Sku = sku,
        Quantity = quantity,
    });

    return Results.Accepted($"/fulfillment/orders/{orderId}/pick-pack");
});

app.MapPost("/search/indexes/{indexName}/sync", async (string indexName, string reason, SearchIndexSyncPublisher publisher) =>
{
    await publisher.Publish(new SearchIndexSyncRequested
    {
        IndexName = indexName,
        Reason = reason,
        RequestedAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/search/indexes/{indexName}/sync");
});

app.MapPost("/catalog/exports/{exportId}/started", async (Guid exportId, string market, CatalogExportLifecyclePublisher publisher) =>
{
    await publisher.Publish(new CatalogExportStarted
    {
        ExportId = exportId,
        Market = market,
        StartedAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/catalog/exports/{exportId}");
});

app.MapPost("/catalog/exports/{exportId}/completed", async (Guid exportId, string market, int itemCount, CatalogExportLifecyclePublisher publisher) =>
{
    await publisher.Publish(new CatalogExportCompleted
    {
        ExportId = exportId,
        Market = market,
        ItemCount = itemCount,
        CompletedAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/catalog/exports/{exportId}/result");
});

app.MapPost("/pricing/quotes", async (string sku, int quantity, PricingQuoteRequester requester) =>
{
    var response = await requester.Request(new PricingQuoteRequested
    {
        Sku = sku,
        Quantity = quantity,
        RequestedAt = DateTimeOffset.UtcNow,
    });

    return Results.Ok(response);
});

app.MapPost("/compliance/decisions/{caseId}", async (string caseId, bool approved, ComplianceDecisionPublisher publisher) =>
{
    await publisher.Publish(new ComplianceDecisionEnvelope
    {
        CaseId = caseId,
        Decision = approved ? "approved" : "rejected",
        DecidedAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/compliance/decisions/{caseId}");
});

app.MapPost("/inventory/{region}/adjusted", async (string region, string sku, int delta, GeoInventoryAdjustedPublisher publisher) =>
{
    await publisher.Publish(region, new InventoryAdjusted
    {
        Region = region,
        Sku = sku,
        Delta = delta,
        AdjustedAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/inventory/{region}/adjusted");
});

app.MapPost("/notifications/digests", async (Guid customerId, NotificationDigestRequester requester) =>
{
    var response = await requester.Request(new NotificationDigestRequested
    {
        CustomerId = customerId,
        RequestedAt = DateTimeOffset.UtcNow,
    });

    return Results.Ok(response);
});

app.MapPost("/tenants/{tenantId}/catalog/rebuilt", async (string tenantId, int productCount, TenantCatalogPublisher publisher) =>
{
    await publisher.Publish(new TenantCatalogRebuilt
    {
        TenantId = tenantId,
        ProductCount = productCount,
        RebuiltAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/tenants/{tenantId}/catalog");
});

app.MapPost("/partners/{partnerId}/exports", async (string partnerId, string exportType, IPartnerExportPublisher publisher) =>
{
    await publisher.Publish(new PartnerExportRequested
    {
        PartnerId = partnerId,
        ExportType = exportType,
        RequestedAt = DateTimeOffset.UtcNow,
    });

    return Results.Accepted($"/partners/{partnerId}/exports");
});

app.MapAsyncApiDocuments();
app.MapAsyncApiUi();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MassTransitUseCases");
app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var address in app.Urls)
    {
        logger.LogInformation("AsyncAPI doc available at: {URL}", $"{address}/asyncapi/asyncapi.json");
        logger.LogInformation("AsyncAPI UI available at: {URL}", $"{address}/asyncapi/ui/");
    }
});

app.Run();
