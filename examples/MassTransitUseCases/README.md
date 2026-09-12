# MassTransit Use Cases

This example collects a broad set of Saunter + MassTransit authoring patterns in one runnable project.

It is not the minimal getting-started path. Start with `examples/MassTransitMinimal` if you want the smallest possible setup, then use this project when you want concrete examples of different annotation styles and messaging shapes.

## Included Use Cases

- `CatalogPriceChangedPublisher` and `CatalogPriceChangedConsumer`: method-level happy-path publish/consume with inference.
- `InventoryReservationRequester`: request client producer with explicit channel id, channel parameters, rich channel tags, explicit message metadata, headers, correlation id, `JsonStringEnumMemberName` wire values, and success/error reply variants.
- `InventoryReservationConsumer`: request/reply consumer that returns a rejection for non-positive quantities and documents both outcomes on a dynamically addressed AsyncAPI 3 reply channel.
- `BillingLifecyclePublisher`: class-level send operation that groups multiple message contracts into one producer boundary.
- `AccountingEventsConsumer`: class-level receive operation that groups multiple consumer message contracts into one consumer boundary.
- `FulfillmentCommandSender` + `PickPackRequestedConsumer`: direct send-to-endpoint command flow instead of publish/subscribe.
- `SearchIndexSyncPublisher`: reusable AsyncAPI channel, operation, and message binding references.
- `AsyncApi/CommerceAsyncApiDocument.cs`: reusable AMQP server binding reference on the documented RabbitMQ server.
- `AsyncApi/CommerceAsyncApiDocument.cs`: direct document-authored AMQP channel, operation, and message bindings without `BindingsRef`.
- `CatalogExportLifecyclePublisher`: one producer method that can emit multiple message variants on one channel.
- `PricingQuoteRequester` + `PricingQuoteConsumer`: request/reply with a statically documented reply channel address.
- `ICustomerPreferenceChangedConsumer` + `CustomerPreferenceChangedConsumer`: interface-based receive-side annotations.
- `OrderProjectionProcessor`: a processor boundary that documents both receive and send operations on one method.
- `ComplianceDecisionPublisher`: one CLR payload shape documented as multiple semantic AsyncAPI messages via distinct message keys.
- `GeoInventoryAdjustedPublisher`: simple string-based channel/operation/message tags plus explicit channel-parameter location metadata.
- `NotificationDigestRequester` + `NotificationDigestConsumer`: request/reply where the logical reply channel is documented but no fixed reply address is declared.
- `TenantCatalogPublisher`: custom `IChannelResolver` usage.
- `PartnerExportRequested.PartnerId`: portable `StringLength` and `Description` annotations appear as property constraints in the generated schema. Byte limits still require an application evaluator.
- `IPartnerExportPublisher` + `PartnerExportPublisher`: interface-based annotation discovery plus named `ChannelId` override on the one-argument `Channel` attribute.
- `LoyaltyPointsAwarded.Effects`: a nested abstract `JsonPolymorphic` hierarchy with string `JsonDerivedType` tags. Each generated `oneOf` alternative requires its literal `$type` and includes the subtype fields; the consumer reads the typed tier effect. Concrete subtype serialization stays independent of base-type tags.
- `LoyaltyPointsAwardedConsumer`: convention-based MassTransit consumer discovery — no AsyncAPI attributes at all; documented because `Discovery.DiscoverMassTransitConsumers` is enabled.

Every producer and consumer class also carries short inline `Use case:` comments directly above the annotated boundary methods, so you can understand the intent without flipping back to this README.

## Start Here

Read the project in this order:

1. `Program.cs`
2. `Configuration/AsyncApiServiceCollectionExtensions.cs`
3. `AsyncApi/CommerceAsyncApiDocument.cs`
4. `Producers/`
5. `Consumers/`
6. `Resolvers/TenantCatalogChannelResolver.cs`

## Running

From the repository root:

```bash
npm install --prefix src/Saunter.UI

dotnet run --project examples/MassTransitUseCases
```

Open:

- `http://localhost:5002/asyncapi/asyncapi.json`
- `http://localhost:5002/asyncapi/ui/`

## Notes

The sample runs on MassTransit's in-memory transport so it works locally without external infrastructure. The generated AsyncAPI document also includes a RabbitMQ-style server entry to show richer server metadata, an AsyncAPI AMQP server binding, channel-level server references, and direct document-authored AMQP channel/operation/message bindings.
