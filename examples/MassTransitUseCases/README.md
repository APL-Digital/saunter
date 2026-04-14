# MassTransit Use Cases

This example collects a broad set of Saunter + MassTransit authoring patterns in one runnable project.

It is not the minimal getting-started path. Start with `examples/MassTransitMinimal` if you want the smallest possible setup, then use this project when you want concrete examples of different annotation styles and messaging shapes.

## Included Use Cases

- `CatalogPriceChangedPublisher` and `CatalogPriceChangedConsumer`: method-level happy-path publish/consume with inference.
- `InventoryReservationRequester`: request client producer with explicit channel id, channel parameters, rich channel tags, explicit message metadata, headers, correlation id, and reply metadata.
- `InventoryReservationConsumer`: request/reply consumer that documents the reply channel generated for AsyncAPI 3.
- `BillingLifecyclePublisher`: class-level send operation that groups multiple message contracts into one producer boundary.
- `AccountingEventsConsumer`: class-level receive operation that groups multiple consumer message contracts into one consumer boundary.
- `FulfillmentCommandSender` + `PickPackRequestedConsumer`: direct send-to-endpoint command flow instead of publish/subscribe.
- `SearchIndexSyncPublisher`: reusable AsyncAPI channel, operation, and message binding references.
- `CatalogExportLifecyclePublisher`: one producer method that can emit multiple message variants on one channel.
- `PricingQuoteRequester` + `PricingQuoteConsumer`: request/reply with a statically documented reply channel address.
- `ICustomerPreferenceChangedConsumer` + `CustomerPreferenceChangedConsumer`: interface-based receive-side annotations.
- `OrderProjectionProcessor`: a processor boundary that documents both receive and send operations on one method.
- `ComplianceDecisionPublisher`: one CLR payload shape documented as multiple semantic AsyncAPI messages via distinct message keys.
- `GeoInventoryAdjustedPublisher`: simple string-based channel/operation/message tags plus explicit channel-parameter location metadata.
- `NotificationDigestRequester` + `NotificationDigestConsumer`: request/reply where the logical reply channel is documented but no fixed reply address is declared.
- `TenantCatalogPublisher`: custom `IChannelResolver` usage.
- `IPartnerExportPublisher` + `PartnerExportPublisher`: interface-based annotation discovery plus named `ChannelId` override on the one-argument `Channel` attribute.

Every producer and consumer class now also carries short inline `Use case:` comments directly above the annotated boundary methods, so you can understand the intent without flipping back to this README.

## Start Here

Read the project in this order:

1. `Program.cs`
2. `Configuration/AsyncApiServiceCollectionExtensions.cs`
3. `AsyncApi/CommerceAsyncApiDocument.cs`
4. `Producers/`
5. `Consumers/`
6. `Resolvers/TenantCatalogChannelResolver.cs`

## Running

```bash
cd ~/saunter/src/Saunter.UI
npm install

cd ~/saunter/examples/MassTransitUseCases
dotnet run
```

Open:

- `http://localhost:5002/asyncapi/asyncapi.json`
- `http://localhost:5002/asyncapi/ui/`

## Notes

The sample runs on MassTransit's in-memory transport so it works locally without external infrastructure. The generated AsyncAPI document also includes a RabbitMQ-style server entry to show richer server metadata and channel-level server references.
