# Saunter

![CI](https://github.com/APL-Digital/saunter/actions/workflows/ci.yaml/badge.svg)

Saunter is a code-first [AsyncAPI](https://www.asyncapi.com/) documentation generator for .NET. It generates AsyncAPI 3.0.0 documents from attributes on your messaging code and serves them (plus an interactive UI) from your ASP.NET Core application.

## Getting Started

For a task-oriented walkthrough with do's and don'ts and a use case cookbook, see
the [Usage Guide](docs/usage-guide.md).

Start with one of these examples:

- [examples/MassTransitMinimal](examples/MassTransitMinimal) for the happy path and inferred defaults
- [examples/MassTransitStreetlights](examples/MassTransitStreetlights) for the advanced, spec-shaped MassTransit example
- [examples/MassTransitUseCases](examples/MassTransitUseCases) for a broader set of MassTransit + Saunter authoring patterns in one project
- [examples/StreetlightsAPI](examples/StreetlightsAPI) for the non-MassTransit Streetlights sample
- [examples/MultiDocument](examples/MultiDocument) for multi-document hosting, filters, `PropertyNameSelector`, and custom inference generators

1. Install the package.

   ```bash
   dotnet add package Apollo.Saunter
   ```

2. Register Saunter and map the endpoints. This is the whole minimal setup:

   ```csharp
   using Saunter;

   builder.Services.AddAsyncApiSchemaGeneration();
   // ...
   app.MapAsyncApi();
   ```

   The defaults do the rest:

   - the **entry assembly** is scanned for `[AsyncApi]` types (set `AssemblyMarkerTypes` only when annotated types live in other assemblies — one marker type *per assembly* is sufficient; markers identify assemblies to scan, not the types to document)
   - the `asyncapi` version is `3.0.0`
   - `info.title`/`info.version` default from the scanned assembly's name and version
   - the UI title falls back to `info.title`
   - the document is served at `/asyncapi/asyncapi.json` (plus a YAML sibling) and the UI at `/asyncapi/ui/`
   - the mapped routes are logged at startup

   Most applications will still want to describe themselves explicitly:

   ```csharp
   services.AddAsyncApiSchemaGeneration(options =>
   {
       options.AsyncApi = new AsyncApiDocumentDescriptor
       {
           Info = new AsyncApiInfoDescriptor
           {
               Title = "Streetlights API",
               Version = "1.0.0",
               Description = "The Smartylighting Streetlights API allows you to remotely manage the city lights.",
               License = new AsyncApiLicenseDescriptor
               {
                   Name = "Apache 2.0",
                   Url = new("https://www.apache.org/licenses/LICENSE-2.0"),
               }
           },
           Servers =
           {
               ["mqtt"] = new AsyncApiServerDescriptor { Host = "test.mosquitto.org", Protocol = "mqtt" },
               // Or build one from a broker connection URI:
               // ["rabbitmq"] = AsyncApiServerDescriptor.FromUri(new Uri("rabbitmq://guest:guest@localhost:5672/")),
           }
       };
   });
   ```

3. Annotate the messaging boundary.

   ```csharp
   using Saunter.AttributeProvider.Attributes;

   [AsyncApi]
   public class StreetlightMessageBus : IStreetlightMessageBus
   {
       [Channel("subscribe/light/measured")]
       [ReceiveOperation]
       public void ReceiveLightMeasurement(LightMeasuredEvent lightMeasuredEvent) { }
   }
   ```

   In the happy path, Saunter infers:

   - `channelId` from the channel address
   - `OperationId` from the member name
   - payload type from the method signature
   - message key, name, and title from the payload type

   If two addresses infer the same `channelId`, override it explicitly:

   ```csharp
   [Channel("system.command.route.*.*.*.*", ChannelId = "commandRouteExtended")]
   [SendOperation(typeof(CommandEnvelope))]
   public void Publish(CommandEnvelope command) { }
   ```

4. Prefer method-level annotations by default.

   Method-level annotations are the clearest path for most users. Class-level annotations are still supported when you want to declare shared channels or shared operation context across multiple members.

5. Open the JSON document. A YAML sibling is served next to every JSON route (`/asyncapi/asyncapi.yaml`).

   ```jsonc
   // GET /asyncapi/asyncapi.json
   {
     "asyncapi": "3.0.0",
     "info": {
       "title": "Streetlights API",
       "version": "1.0.0"
     },
     "channels": {
       "lightMeasured": {
         "address": "subscribe/light/measured"
       }
     },
     "operations": {
       "ReceiveLightMeasurement": {
         "action": "receive",
         "channel": {
           "$ref": "#/channels/lightMeasured"
         }
       }
     }
   }
   ```

6. Open the UI.

   ![AsyncAPI UI](assets/asyncapi-ui-screenshot.png)

## Annotation Mental Model

- Put Saunter annotations on the messaging boundary, not the HTTP boundary.
- Annotate producer methods and consumer methods.
- Keep controllers and adapters thin.
- Use `[Message]` only when you need to override inferred message metadata.
- Use class-level annotations only when shared declaration is genuinely clearer than method-level placement.

The [Usage Guide](docs/usage-guide.md) expands each of these into concrete do's and don'ts, plus a 15-recipe use case cookbook.

## Configuration

See [AsyncApiOptions](src/Saunter/Options/AsyncApiOptions.cs) for detailed info.

```csharp
services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = new[] { typeof(Startup) };
    options.AddChannelFilter<MyChannelFilter>();
    options.AddOperationFilter<MyOperationFilter>();
    options.PropertyNameSelector = property =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
        ?? property.Name;
    options.Middleware.Route = "/asyncapi/asyncapi.json";
    options.Middleware.UiBaseRoute = "/asyncapi/ui/";
    options.Middleware.UiTitle = "My AsyncAPI Documentation";
    options.Inference.InferOperationIdFromMemberName = true;
    options.Inference.InferChannelIdFromAddress = true;
    options.Inference.InferPayloadTypeFromMethodSignature = true;
    options.Inference.OperationIdGenerator = (member, action) => member.Name;
    options.Inference.ChannelIdGenerator = address => "myCustomChannelId";
    options.ValidateOnStartup = true;
});
```

`ValidateOnStartup` generates every registered document once at startup so misconfiguration (duplicate operation ids, unresolved references) fails fast with a descriptive exception instead of a 500 on the first request. It defaults to on in the Development environment only; set it to `true`/`false` to control it explicitly.

A document that ends up with no channels and no operations logs a warning naming the scanned assemblies and the attribute document name that was matched, so a missing marker type or a mismatched `[AsyncApi("name")]` is visible instead of silently producing an empty document. Requesting a document name that is neither configured nor matched by any attribute throws a descriptive exception listing the known documents.

## MassTransit Consumer Discovery

Unannotated MassTransit `IConsumer<T>` implementations can be documented by convention, without any Saunter attributes:

```csharp
services.AddAsyncApiSchemaGeneration(options =>
{
    options.Discovery.DiscoverMassTransitConsumers = true;
});
```

For every discovered consumer, Saunter emits a receive operation per consumed message type:

- the channel address defaults to the message type's `Namespace:TypeName` — MassTransit's message topology (MessageUrn) naming, i.e. the exchange/topic a published message targets. Set `Discovery.MassTransitChannelAddressGenerator` to change it (e.g. `AsyncApiDiscoveryOptions.KebabCaseEndpointAddress` for queue-style names)
- the operation id is `{ConsumerName}.{MessageName}.receive`
- `Discovery.MassTransitConsumerFilter` can exclude specific consumer types

Consumers that already carry AsyncAPI attributes are skipped, so discovery is a low-ceremony floor: annotate a consumer whenever you need reply metadata, explicit channels, headers, or versioned message contracts, and the annotation wins. Only the consume side is discoverable — publishes remain attribute-driven (see the publication-marker pattern in [examples/MassTransitUseCases](examples/MassTransitUseCases)). Discovery is reflection-only; Saunter takes no MassTransit package dependency.

Default inference decisions:

- inferred operation ids preserve the member name casing
- inferred channel ids use the configured `ChannelIdGenerator`
- schema property names honor `[JsonPropertyName]`; `PropertyNameSelector` can override that globally

Related authoring surface:

- richer channel tag metadata can be declared with `[ChannelTag(...)]`
- channel parameters support `DefaultValue` and `Examples`
- the package ships Roslyn analyzers that flag common annotation mistakes at build time

## Bindings

Bindings can be referenced from attributes through `BindingsRef`, and document descriptors can also declare bindings inline on servers, channels, operations, and component messages.

```csharp
using ByteBard.AsyncAPI.Bindings.AMQP;
using ByteBard.AsyncAPI.Bindings.Http;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Models.Interfaces;
using Saunter.Bindings.AMQP;
using Saunter.AttributeProvider.Descriptors;

services.AddAsyncApiSchemaGeneration(options =>
{
    options.AsyncApi = new AsyncApiDocumentDescriptor
    {
        Servers =
        {
            ["rabbitmq"] = new AsyncApiServerDescriptor
            {
                Host = "broker.example.com:5671",
                Protocol = "amqps",
                Bindings = new AsyncApiBindings<IServerBinding>
                {
                    new AMQPServerBinding()
                }
            }
        },
        Components =
        {
            ServerBindings =
            {
                ["sharedRabbitMq"] = new()
                {
                    new AMQPServerBinding()
                }
            },
            Messages =
            {
                ["signupMessage"] = new AsyncApiMessageDescriptor
                {
                    Id = "signupMessage",
                    Name = "signupMessage",
                    Title = "Signup event",
                    PayloadSchemaId = "signupPayload",
                    Bindings = new()
                    {
                        new AMQPMessageBinding
                        {
                            ContentEncoding = "gzip",
                            MessageType = "user.signup",
                        }
                    }
                }
            },
            ChannelBindings =
            {
                ["amqpDev"] = new()
                {
                    new AMQPChannelBinding
                    {
                        Is = ChannelType.Queue,
                        Exchange = new()
                        {
                            Name = "example-exchange",
                            Vhost = "/development"
                        }
                    }
                }
            },
            OperationBindings =
            {
                ["postBind"] = new()
                {
                    new HttpOperationBinding
                    {
                        Method = "POST",
                        Type = HttpOperationBinding.HttpOperationType.Response,
                    }
                }
            }
        },
        Channels =
        {
            ["routedChannel"] = new AsyncApiChannelDescriptor
            {
                Id = "routedChannel",
                Address = "user.signup",
                ServerNames = ["rabbitmq"],
                MessageIds = ["signupMessage"],
                Bindings = new()
                {
                    new AMQPChannelBinding
                    {
                        Is = ChannelType.RoutingKey,
                        Exchange = new()
                        {
                            Name = "user.events",
                            Type = ExchangeType.Topic,
                        }
                    }
                }
            }
        }
    };
});
```

`AsyncApiServerDescriptor` supports both inline `Bindings` and `BindingsRef`. Attribute-based `Channel`, `Operation`, and `Message` annotations still use `BindingsRef`; inline bindings for those shapes are currently a document-descriptor feature.

## Multiple AsyncAPI Documents

```csharp
services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = new[] { typeof(FleetPublisher), typeof(ConfigPublisher) };
});

services.ConfigureAsyncApiDocument("fleet", document =>
{
    document.AttributeDocumentName = "v1";
    document.MarkerTypes.Add(typeof(FleetPublisher));
    document.Document.Info = new AsyncApiInfoDescriptor { Title = "Fleet API", Version = "1.0.0" };
});

services.ConfigureAsyncApiDocument("config", document =>
{
    document.AttributeDocumentName = "v1";
    document.MarkerTypes.Add(typeof(ConfigPublisher));
    document.Document.Info = new AsyncApiInfoDescriptor { Title = "Config Messaging API", Version = "1.0.0" };
});
```

Each registration derives its routes from the document name automatically — `fleet` is served at `/asyncapi/fleet/asyncapi.json` (plus a YAML sibling) with the UI at `/asyncapi/fleet/ui` — and the UI title falls back to the document's `info.title`. Set `document.Middleware.Route`/`UiBaseRoute`/`UiTitle` only to override those defaults.

Use `ConfigureAsyncApiDocument(...)` when you need independent hosted documents with their own:

- route
- UI base route
- title
- marker type set
- type filter

This is the preferred model when:

- two hosted documents reuse the same `[AsyncApi("...")]` document name
- one process co-hosts multiple messaging boundaries
- you need stable, non-templated URLs such as `/asyncapi/fleet/ui`

You can also split a single attribute document name into multiple hosted documents with `TypeFilter`:

```csharp
services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = new[] { typeof(OrdersV1Publisher), typeof(InvoicesV1Publisher) };
});

services.ConfigureAsyncApiDocument("orders-v1", document =>
{
    document.AttributeDocumentName = "v1";
    document.TypeFilter = type => type.AsType() == typeof(OrdersV1Publisher);
});

services.ConfigureAsyncApiDocument("invoices-v1", document =>
{
    document.AttributeDocumentName = "v1";
    document.TypeFilter = type => type.AsType() == typeof(InvoicesV1Publisher);
});
```

Saunter still supports `ConfigureNamedAsyncApi(...)` for the legacy model where the route template contains `{document}` and the hosted document key matches the `[AsyncApi("...")]` document name. Prefer `ConfigureAsyncApiDocument(...)` for new work.

## Migrating From Saunter 0.x (AsyncAPI 2.x)

This fork generates AsyncAPI 3.0.0 documents. If you are coming from upstream Saunter (AsyncAPI 2.x), the main changes are:

- LEGO AsyncAPI.NET was replaced with `ByteBard.AsyncAPI.NET`, `ByteBard.AsyncAPI.NET.Readers`, and `ByteBard.AsyncAPI.NET.Bindings`.
- Public API types now use Saunter descriptors, including `AsyncApiOptions.AsyncApi`, `IAsyncApiDocumentProvider`, and the filter interfaces.
- `PublishOperationAttribute` and `SubscribeOperationAttribute` were replaced with `SendOperationAttribute` and `ReceiveOperationAttribute`:

  ```csharp
  [Channel("temperature.sensor", "sensors/temperature")]
  [SendOperation(typeof(TemperatureReading))]
  public void PublishTemperature(TemperatureReading reading) { }

  [Channel("temperature.sensor", "sensors/temperature")]
  [ReceiveOperation(typeof(TemperatureReading))]
  public void ConsumeTemperature(TemperatureReading reading) { }
  ```

- Generated documents use AsyncAPI v3 root `operations` and channel `address` fields instead of v2 channel-local `publish` and `subscribe`. Code that mutates or asserts the v2 document shape (e.g. document filters) must be updated to the v3 model.

## Contributing

See the [contributing guide](CONTRIBUTING.md).

## Thanks

- This project is heavily inspired by [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore).
- We use [ByteBard AsyncAPI.NET](https://github.com/ByteBardOrg/AsyncAPI.NET) for schema modeling and serialization.
