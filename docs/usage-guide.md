# Saunter Usage Guide

Saunter is a code-first [AsyncAPI 3.0.0](https://www.asyncapi.com/) documentation
generator for .NET. You annotate the messaging boundary of your ASP.NET Core
application; Saunter scans those annotations at runtime and serves an AsyncAPI
document (JSON + YAML) plus an interactive UI.

This guide covers how to use the library well and lists the do's and don'ts that
keep generated documents correct and stable. For the API-level walkthrough see
[README.md](../README.md); for build-time analyzer rules see
[analyzers.md](analyzers.md).

## Mental model

- An AsyncAPI document is a set of **channels** (addresses/topics/queues),
  **operations** (`send` / `receive` actions bound to a channel), and
  **messages** (payload + header schemas).
- Saunter builds that document from attributes on your messaging code — not from
  your HTTP controllers.
- The happy path is inference: annotate a method with `[Channel]` and
  `[SendOperation]` / `[ReceiveOperation]`, and Saunter derives the channel id,
  operation id, payload schema, and message metadata for you.
- You override inference only where the defaults are wrong or ambiguous.

## Quick start

1. Install the package.

   ```bash
   dotnet add package Apollo.Saunter
   ```

2. Register generation and map the endpoints. **This is the whole minimal
   setup — no options lambda required.**

   ```csharp
   using Saunter;

   builder.Services.AddAsyncApiSchemaGeneration();
   // ...
   app.MapAsyncApi();
   ```

   The defaults carry you the rest of the way:

   - the **entry assembly** is scanned for `[AsyncApi]` types
   - `asyncapi` version is `3.0.0`; `info.title`/`info.version` default from the
     scanned assembly's name and version; the UI title falls back to `info.title`
   - the document is served at `/asyncapi/asyncapi.json` (plus a YAML sibling) and
     the UI at `/asyncapi/ui/`
   - all inference (channel id, operation id, payload schema, message name/title)
     is **on by default**

3. Annotate the messaging boundary — bare attributes are enough.

   ```csharp
   using Saunter.AttributeProvider.Attributes;

   [AsyncApi]
   public class StreetlightMessageBus
   {
       [Channel("subscribe/light/measured")]
       [ReceiveOperation]
       public void ReceiveLightMeasurement(LightMeasuredEvent lightMeasuredEvent) { }
   }
   ```

   No `ChannelId`, no `OperationId`, no `typeof`, no `[Message]`: Saunter infers the
   channel id from the address, the operation id from the member name, and the
   payload schema from the method signature (the `ConsumeContext<T>` type argument
   here, or the single message-shaped parameter for a producer).

   > **MassTransit users get an even shorter path.** Turn on
   > `options.Discovery.DiscoverMassTransitConsumers = true` and your `IConsumer<T>`
   > implementations are documented with **zero attributes**. See
   > [MassTransit consumer discovery](#masstransit-consumer-discovery).

4. Browse the results:
   - Document: `GET /asyncapi/asyncapi.json` (YAML sibling at `/asyncapi/asyncapi.yaml`)
   - UI: `/asyncapi/ui/`

## Configure only when you need to

Saunter is designed so that each piece of configuration is opt-in. Add it only
when you hit the matching need — not preemptively.

| Start here (least config) | Add this only when… |
|---------------------------|---------------------|
| `AddAsyncApiSchemaGeneration()` with no lambda | you want a real `info.title`/`version`, a `Servers` map, or a license → set `options.AsyncApi` |
| Bare `[Channel]` + `[SendOperation]`/`[ReceiveOperation]` | an inferred id collides or reads badly → set `ChannelId`/`OperationId` |
| Inferred payload from the method signature | the method signature isn't the payload, or you need several messages → add `[Message]` |
| Entry-assembly scanning | annotated types live in other assemblies → set `AssemblyMarkerTypes` (one marker **per assembly**) |
| A single hosted document | you co-host multiple messaging boundaries → `ConfigureAsyncApiDocument(...)` |

Most apps *will* eventually want to describe themselves — a title, version, and at
least one server make the UI far more useful:

```csharp
builder.Services.AddAsyncApiSchemaGeneration(options =>
{
    options.AsyncApi = new AsyncApiDocumentDescriptor
    {
        Info = new AsyncApiInfoDescriptor { Title = "Streetlights API", Version = "1.0.0" },
        Servers = { ["mqtt"] = new AsyncApiServerDescriptor { Host = "test.mosquitto.org", Protocol = "mqtt" } },
    };
});
```

But treat that as the *second* step, not a prerequisite.

## The core attributes

| Attribute | Applies to | Purpose |
|-----------|-----------|---------|
| `[AsyncApi]` | class / interface | Marks a type as containing AsyncAPI annotations. Optional document name argument groups types into a named document. |
| `[Channel("address")]` | method / class / interface | Declares a channel and its address. |
| `[SendOperation]` / `[ReceiveOperation]` | method / class / interface | Declares the operation action bound to the channel on the same member. |
| `[Message(typeof(T))]` | method (repeatable) | Overrides or adds message metadata; use only when inference is insufficient. |
| `[ChannelParameter("name")]` | method / class / interface (repeatable) | Describes a `{name}` segment in a channel address. |
| `[ChannelTag(...)]` | method / class / interface (repeatable) | Adds channel tag metadata. |

**`[Channel]` has three constructors — mind the argument order.** The first
positional argument is the *address* in the one-arg form but the *channel id* in
the others:

| Overload | Signature | Use |
|----------|-----------|-----|
| One-arg | `[Channel(address)]` | Address only; channel id is inferred (or set via `ChannelId = ...`). |
| Two-arg | `[Channel(channelId, address)]` | Explicit channel id **first**, then address. |
| Three-arg | `[Channel(channelId, typeof(IChannelResolver), typeof(payload))]` | Channel id, then a resolver that computes the address from the payload type. |

So `[Channel("orders/created")]` sets the *address*, but
`[Channel("ordersCreated", "orders/created")]` sets the *id* then the address —
they are not interchangeable.

`[SendOperation]` / `[ReceiveOperation]` can take the payload type explicitly
(`[SendOperation(typeof(CommandEnvelope))]`) or infer it from the method
signature: Saunter unwraps a `ConsumeContext<T>` parameter to `T`, otherwise it
uses the single message-shaped parameter (ignoring `string`, `Guid`, `DateTime`,
`CancellationToken`, and other primitives). If more than one candidate parameter
remains, nothing is inferred and you must pass the type explicitly.

## Do's and don'ts

### Placement

✅ **Do** put annotations on the messaging boundary — the producer methods that
publish and the consumer methods that handle messages.

✅ **Do** keep controllers, adapters, and transport plumbing thin and
un-annotated.

❌ **Don't** annotate your HTTP controllers or REST endpoints. AsyncAPI describes
event/message flows, not request/response HTTP.

❌ **Don't** annotate DTOs or contract classes with the operation attributes —
those describe *operations*, and belong on methods.

### Inference vs. explicit values

✅ **Do** lean on inference for the happy path. Let Saunter derive `channelId`
from the address, `OperationId` from the member name, and the payload/message
metadata from the method signature.

✅ **Do** override an inferred value only when it is wrong or collides — for
example when two addresses infer the same `channelId`:

```csharp
[Channel("system.command.route.*.*.*.*", ChannelId = "commandRouteExtended")]
[SendOperation(typeof(CommandEnvelope))]
public void Publish(CommandEnvelope command) { }
```

❌ **Don't** hand-write ids and names everywhere "to be explicit." Redundant
overrides drift out of sync with the code and add noise.

❌ **Don't** rely on inference and then also disable the matching inference option
in `options.Inference` — the values will silently go missing.

### Operation ids must be unique

✅ **Do** keep every `OperationId` unique within a document — they are the keys of
the root `operations` map. Prefer member-name inference so each method
contributes its own id.

❌ **Don't** give two operations the same literal `OperationId`. The analyzer flags
this as **SAUN001** at build time; inferred collisions (e.g. two method overloads
with the same name) fail at document-generation time instead.

### Schema ids must be unique

✅ **Do** set `[Message(..., PayloadSchemaId = "...")]` when two payload types share
the same simple type name and renaming the CLR type isn't an option (e.g. it would
change the wire name):

```csharp
[Message(typeof(Orders.Created), PayloadSchemaId = "ordersCreated")]
[Message(typeof(Billing.Created), PayloadSchemaId = "billingCreated")]
```

❌ **Don't** let two distinct payload types with the same simple name generate the
same schema id — Saunter rejects the collision.

### Channel parameters

✅ **Do** describe every `{name}` segment in an address with a matching
`[ChannelParameter("name")]`.

❌ **Don't** declare a `[ChannelParameter]` whose name isn't present as `{name}` in
the address, and don't leave an address parameter undescribed. The analyzer flags
mismatches as **SAUN003**; invalid parameter names as **SAUN006**.

### `[Message]` usage

✅ **Do** use `[Message]` to *override* inferred message metadata (name, title,
headers, content type, correlation id) or to declare multiple messages on one
channel.

❌ **Don't** add a bare `[Message]` without a `[SendOperation]` / `[ReceiveOperation]`
on the method or containing type — an orphaned message annotation does nothing and
is flagged as **SAUN005**.

❌ **Don't** use a non-absolute URL in `[Message(ExternalDocs = "...")]`. It must be a
fully qualified URI (**SAUN002**).

### Reply (request/reply) operations

✅ **Do** set `Reply` to the reply channel id whenever you configure any other
`Reply*` property.

✅ **Do** pick at most one of `ReplyChannelAddress` (explicit address) or
`ReplyAddressLocation` (dynamic runtime expression like `$message.header#/replyTo`).

❌ **Don't** set both `ReplyChannelAddress` and `ReplyAddressLocation` — they are
mutually exclusive (**SAUN007**).

### Reference names

✅ **Do** use only letters, digits, `.`, `-`, or `_` in reference-valued properties
(`OperationId`, `BindingsRef`, `Reply`, `CorrelationId`, `MessageId`, `Servers`).
Invalid characters are flagged as **SAUN004**.

### Class-level vs. method-level

✅ **Do** prefer method-level annotations by default — they are the clearest and
keep each operation self-contained.

✅ **Do** use class/interface-level annotations only when you genuinely want to
share a channel or operation context across multiple members.

❌ **Don't** reach for class-level annotations just to reduce line count; it
obscures which method maps to which operation.

### Marker types & assembly scanning

✅ **Do** rely on the entry-assembly default when all annotated types live in your
main project.

✅ **Do** set `options.AssemblyMarkerTypes` when annotated types live in *other*
assemblies — one marker type **per assembly** is enough. Markers identify
assemblies to scan, not the types to document.

❌ **Don't** add one marker per annotated type. That's unnecessary and misreads the
purpose of markers.

### Validation & empty documents

✅ **Do** leave `ValidateOnStartup` on (it defaults to on in Development). It
generates every document once at startup so duplicate ids and unresolved
references fail fast with a descriptive exception instead of a 500 on first
request. Set it to `true` explicitly to enforce it in all environments.

✅ **Do** check startup logs. A document with no channels and no operations logs a
warning naming the scanned assemblies and matched document name — usually a
missing marker type or a mismatched `[AsyncApi("name")]`.

❌ **Don't** request a document name that is neither configured nor matched by any
attribute — it throws a descriptive exception listing the known documents.

## Use case cookbook

Concrete, trimmed recipes for the patterns you'll reach for most. Every recipe
here has a full, runnable counterpart in
[examples/MassTransitUseCases](../examples/MassTransitUseCases) — the named class
in each recipe points at the source file.

### 1. Publish an event (fire-and-forget)

The happy path: two bare attributes, no `typeof`. Saunter infers the operation id
from the method name and the payload schema from the method's single
message-shaped parameter. (`CatalogPriceChangedPublisher`)

```csharp
[AsyncApi]
public class CatalogPriceChangedPublisher
{
    [Channel("catalog.price-changed")]
    [SendOperation]
    public Task Publish(ProductPriceChanged message) => _publishEndpoint.Publish(message);
}
```

Reach for `[SendOperation(typeof(ProductPriceChanged))]` only when the payload
isn't the method's parameter (for example a request/reply method whose parameter
and reply differ).

### 2. Receive/consume a message

Same shape with `[ReceiveOperation]` — still bare. The payload is
`ProductPriceChanged`, not `ConsumeContext<…>`: Saunter unwraps the
`ConsumeContext<T>` parameter to its type argument. (`CatalogPriceChangedConsumer`)

```csharp
[AsyncApi]
public class CatalogPriceChangedConsumer : IConsumer<ProductPriceChanged>
{
    [Channel("catalog.price-changed")]
    [ReceiveOperation]
    public Task Consume(ConsumeContext<ProductPriceChanged> context) => Task.CompletedTask;
}
```

> If this consumer is a MassTransit `IConsumer<T>`, you can often drop the
> attributes entirely and let [discovery](#masstransit-consumer-discovery)
> document it.

### 3. Request/reply with a dynamic reply address

Set `Reply` to the reply channel id and locate the reply address at runtime with
`ReplyAddressLocation`. (`InventoryReservationRequester`)

```csharp
[Channel("inventory.reservations", "inventory/reservations/{warehouseId}")]
[ChannelParameter("warehouseId", typeof(string), Description = "Warehouse that processes the reservation.")]
[SendOperation(typeof(InventoryReservationRequested),
    OperationId = "RequestInventoryReservation",
    Reply = "inventoryReservationsReply",
    ReplyMessagePayloadType = typeof(InventoryReserved),
    ReplyAddressLocation = "$message.header#/responseAddress")]
public Task<InventoryReserved> Request(string warehouseId, InventoryReservationRequested message) => /* ... */;
```

### 4. Request/reply with a fixed reply address

Same idea, but the reply channel has a statically known address. Use
`ReplyChannelAddress` instead of `ReplyAddressLocation` — never both.
(`PricingQuoteRequester`)

```csharp
[SendOperation(typeof(PricingQuoteRequested),
    OperationId = "RequestPricingQuote",
    Reply = "pricingQuoteReplies",
    ReplyChannelAddress = "pricing/quotes/replies",
    ReplyMessagePayloadType = typeof(PricingQuoteReady))]
public Task<PricingQuoteReady> Request(PricingQuoteRequested message) => /* ... */;
```

### 5. Several message variants on one channel

Stack multiple `[Message]` attributes on one method to document every variant it
can emit. (`CatalogExportLifecyclePublisher`)

```csharp
[Channel("catalog.export.lifecycle")]
[SendOperation(OperationId = "PublishCatalogExportLifecycle")]
[Message(typeof(CatalogExportStarted), Name = "CatalogExportStarted", Title = "Catalog export started")]
[Message(typeof(CatalogExportCompleted), Name = "CatalogExportCompleted", Title = "Catalog export completed")]
public Task Publish(object message) => _publishEndpoint.Publish(message);
```

### 6. One payload type, multiple semantic messages

When the same CLR type carries different business meanings, give each `[Message]`
a distinct `MessageId` so they become distinct AsyncAPI messages.
(`ComplianceDecisionPublisher`)

```csharp
[Channel("compliance.decisions")]
[SendOperation(OperationId = "PublishComplianceDecision")]
[Message(typeof(ComplianceDecisionEnvelope), MessageId = "complianceApproved", Name = "ComplianceApproved")]
[Message(typeof(ComplianceDecisionEnvelope), MessageId = "complianceRejected", Name = "ComplianceRejected")]
public Task Publish(ComplianceDecisionEnvelope message) => _publishEndpoint.Publish(message);
```

### 7. Group several contracts behind a class-level operation

Declare the channel and operation once on the class; each method just adds its
`[Message]`. Use this only when the shared boundary is genuinely clearer than
per-method operations. (`BillingLifecyclePublisher`)

```csharp
[AsyncApi]
[Channel("billing.lifecycle")]
[SendOperation(OperationId = "PublishBillingLifecycleEvents")]
public class BillingLifecyclePublisher
{
    [Message(typeof(InvoiceIssued), Name = "InvoiceIssued")]
    public Task PublishInvoiceIssued(InvoiceIssued message) => _publishEndpoint.Publish(message);

    [Message(typeof(InvoicePaid), Name = "InvoicePaid")]
    public Task PublishInvoicePaid(InvoicePaid message) => _publishEndpoint.Publish(message);
}
```

### 8. Declare the contract on an interface

Put the annotations on the interface when multiple implementations may exist; the
implementing class stays annotation-free. (`ICustomerPreferenceChangedConsumer`)

```csharp
[AsyncApi]
public interface ICustomerPreferenceChangedConsumer
{
    [Channel("customer.preferences", ChannelId = "customerPreferences")]
    [ReceiveOperation(typeof(CustomerPreferenceChanged), OperationId = "HandleCustomerPreferenceChanged")]
    [Message(typeof(CustomerPreferenceChanged), Name = "CustomerPreferenceChanged")]
    Task Consume(ConsumeContext<CustomerPreferenceChanged> context);
}
```

### 9. A processor that receives and sends on one method

A single method can carry both a `[ReceiveOperation]` and a `[SendOperation]` to
model a pipeline step. (`OrderProjectionProcessor`)

```csharp
[Channel("order.projection.pipeline")]
[ReceiveOperation(typeof(OrderProjectionRequested), OperationId = "HandleOrderProjectionRequested")]
[SendOperation(typeof(OrderProjectionUpdated), OperationId = "PublishOrderProjectionUpdated")]
public Task Consume(ConsumeContext<OrderProjectionRequested> context) => /* ... */;
```

### 10. Send a command directly to a queue

Document a send-to-endpoint boundary the same way you document a publish — the
attributes describe the AsyncAPI shape, not the MassTransit delivery mode.
(`FulfillmentCommandSender`)

```csharp
[Channel("fulfillment.pick-pack", "queue:fulfillment-pick-pack")]
[SendOperation(typeof(PickPackRequested), OperationId = "SendPickPackRequested")]
[Message(typeof(PickPackRequested), Name = "PickPackRequested", HeadersType = typeof(CommerceMessageHeaders))]
public Task Send(PickPackRequested message) => /* GetSendEndpoint(...).Send(message) */;
```

### 11. Parameterized channel address

Describe every `{segment}` in the address with a `[ChannelParameter]`, including
`DefaultValue`, `Examples`, and `Location`. (`GeoInventoryAdjustedPublisher`)

```csharp
[Channel("inventory/adjusted/{region}", ChannelId = "geoInventoryAdjusted", Tags = new[] { "inventory", "geo" })]
[ChannelParameter("region", typeof(string),
    Description = "Sales region carried in the address and echoed in headers.",
    Location = "$message.header#/region",
    Examples = new[] { "eu-west", "us-east" })]
[SendOperation(typeof(InventoryAdjusted), "inventory", "projection", OperationId = "PublishGeoInventoryAdjusted")]
public Task Publish(string region, InventoryAdjusted message) => /* ... */;
```

### 12. Rich message metadata (headers, correlation id, content type, docs)

`[Message]` carries the full message-object surface when you need it.
(`InventoryReservationRequester`)

```csharp
[Message(typeof(InventoryReservationRequested),
    Name = "InventoryReservationRequested",
    Title = "Inventory reservation requested",
    Summary = "Request that a warehouse reserve inventory for an order.",
    HeadersType = typeof(CommerceMessageHeaders),
    CorrelationId = "workflowCorrelation",
    ContentType = "application/json",
    ExternalDocs = "https://example.com/docs/inventory/reservations/request")]
```

### 13. Resolve the channel address from a custom resolver

Pass an `IChannelResolver` and the payload type to compute the address instead of
hardcoding it. (`TenantCatalogPublisher`)

```csharp
[Channel("tenantCatalogRebuilt", typeof(TenantCatalogChannelResolver), typeof(TenantCatalogRebuilt))]
[SendOperation(typeof(TenantCatalogRebuilt), OperationId = "PublishTenantCatalogRebuilt")]
public Task Publish(TenantCatalogRebuilt message) => _publishEndpoint.Publish(message);
```

### 14. Reference reusable bindings

Attach transport-specific bindings at the channel, operation, and message level
via `BindingsRef`, and register the bindings under matching
`components/*Bindings` names. (`SearchIndexSyncPublisher`)

```csharp
[Channel("search.index.sync", BindingsRef = "searchIndexKafkaTopic")]
[SendOperation(typeof(SearchIndexSyncRequested), OperationId = "PublishSearchIndexSyncRequested", BindingsRef = "searchIndexKafkaProducer")]
[Message(typeof(SearchIndexSyncRequested), Name = "SearchIndexSyncRequested", BindingsRef = "searchIndexKafkaMessage")]
public Task Publish(SearchIndexSyncRequested message) => _publishEndpoint.Publish(message);
```

See [Bindings](#bindings) below for how to register the referenced binding
definitions (and the inline-binding alternative on document descriptors).

### 15. Group and cross-link with channel tags

`[ChannelTag]` adds structured grouping metadata with external-docs links; the
string-array `Tags` on `[Channel]`/operations/messages is the lighter-weight
alternative. (`InventoryReservationRequester`, `GeoInventoryAdjustedPublisher`)

```csharp
[ChannelTag("inventory",
    Description = "Channels used to reserve stock and coordinate inventory workflows.",
    ExternalDocs = "https://example.com/docs/inventory",
    ExternalDocsDescription = "Inventory workflow documentation.")]
```

## MassTransit consumer discovery

If you use MassTransit, unannotated `IConsumer<T>` implementations can be
documented by convention:

```csharp
services.AddAsyncApiSchemaGeneration(options =>
{
    options.Discovery.DiscoverMassTransitConsumers = true;
});
```

Saunter emits one receive operation per consumed message type.

✅ **Do** treat discovery as a low-ceremony floor: leave simple consumers
unannotated, and annotate one whenever you need reply metadata, explicit channels,
headers, or versioned contracts — the annotation wins over discovery.

✅ **Do** set `Discovery.MassTransitChannelAddressGenerator` (e.g.
`AsyncApiDiscoveryOptions.KebabCaseEndpointAddress`) if you want queue-style
addresses instead of the default `Namespace:TypeName` (MessageUrn) naming.

❌ **Don't** expect publishes to be discovered — only the consume side is
discoverable; publishers remain attribute-driven.

❌ **Don't** expect a MassTransit package dependency to be pulled in — discovery is
reflection-only.

## Multiple documents

Use `ConfigureAsyncApiDocument(...)` when one process hosts several messaging
boundaries or when two hosted documents reuse the same `[AsyncApi("name")]`:

```csharp
services.ConfigureAsyncApiDocument("fleet", document =>
{
    document.AttributeDocumentName = "v1";
    document.MarkerTypes.Add(typeof(FleetPublisher));
    document.Document.Info = new AsyncApiInfoDescriptor { Title = "Fleet API", Version = "1.0.0" };
});
```

Each registration derives its routes from the document name automatically —
`fleet` is served at `/asyncapi/fleet/asyncapi.json` with the UI at
`/asyncapi/fleet/ui`.

✅ **Do** prefer `ConfigureAsyncApiDocument(...)` for new work — it gives stable,
non-templated URLs and independent route/title/marker/filter per document.

✅ **Do** use `TypeFilter` to split one attribute document into multiple hosted
documents when needed.

❌ **Don't** reach for the legacy `ConfigureNamedAsyncApi(...)` (with a `{document}`
route template) for new work — it remains supported only for backward
compatibility.

## Bindings

Reference reusable bindings from attributes via `BindingsRef`, and declare
bindings inline on servers, channels, operations, and component messages in the
document descriptor. See the [Bindings section of the README](../README.md#bindings)
for a full example.

✅ **Do** register the referenced bindings under the matching
`components/*Bindings` name that your `BindingsRef` points at.

❌ **Don't** expect inline bindings on attribute-based `[Channel]` / `[Operation]` /
`[Message]` — those use `BindingsRef`. Inline bindings are currently a
document-descriptor feature (server descriptors support both inline and
`BindingsRef`).

## Analyzer rules at a glance

The `Apollo.Saunter` package ships Roslyn analyzers (category `Usage`, severity
`Warning`, enabled by default) that catch annotation mistakes at build time.

| Rule | Meaning |
|------|---------|
| SAUN001 | Duplicate operation id |
| SAUN002 | Invalid external docs URL (must be absolute) |
| SAUN003 | Channel parameter doesn't match address |
| SAUN004 | Invalid reference name (allowed: letters, digits, `.`, `-`, `_`) |
| SAUN005 | Annotation missing its companion (`[Message]`/`[ChannelParameter]`) |
| SAUN006 | Invalid channel parameter name |
| SAUN007 | Invalid operation reply configuration |

See [analyzers.md](analyzers.md) for the full descriptions and fixes.

## Migrating from Saunter 0.x (AsyncAPI 2.x)

This fork generates AsyncAPI **3.0.0**. Key changes:

- `PublishOperationAttribute` / `SubscribeOperationAttribute` →
  `SendOperationAttribute` / `ReceiveOperationAttribute`.
- Documents use v3 root `operations` and channel `address` fields instead of v2
  channel-local `publish` / `subscribe`.

❌ **Don't** keep document filters that mutate or assert the v2 shape — update them
to the v3 model (root `operations`, channel `address`).

See the [migration section of the README](../README.md#migrating-from-saunter-0x-asyncapi-2x)
for details.

## Where to go next

- [examples/MassTransitMinimal](../examples/MassTransitMinimal) — happy path and inferred defaults
- [examples/MassTransitStreetlights](../examples/MassTransitStreetlights) — advanced, spec-shaped MassTransit example
- [examples/MassTransitUseCases](../examples/MassTransitUseCases) — a living reference of current authoring patterns
- [examples/StreetlightsAPI](../examples/StreetlightsAPI) — non-MassTransit sample
- [examples/MultiDocument](../examples/MultiDocument) — multi-document hosting, filters, custom inference
- [AsyncApiOptions](../src/Saunter/Options/AsyncApiOptions.cs) — full configuration surface
</content>
</invoke>
