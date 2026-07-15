# Saunter

![CI](https://github.com/APL-Digital/saunter/actions/workflows/ci.yaml/badge.svg)

Saunter is a code-first [AsyncAPI](https://www.asyncapi.com/) documentation generator for .NET. It generates AsyncAPI 3.0.0 documents from attributes on your messaging code and serves them (plus an interactive UI) from your ASP.NET Core application.

## Getting Started

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

2. Configure Saunter in `ConfigureServices`.

   ```csharp
   using Saunter;

   services.AddAsyncApiSchemaGeneration(options =>
   {
       options.AssemblyMarkerTypes = new[] { typeof(StreetlightMessageBus) };
       options.Middleware.UiTitle = "Streetlights API";

       options.AsyncApi = new AsyncApiDocumentDescriptor
       {
           Asyncapi = "3.0.0",
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

5. Map the endpoints.

   ```csharp
   app.MapAsyncApiDocuments();
   app.MapAsyncApiUi();
   ```

6. Open the JSON document.

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

7. Open the UI.

   ![AsyncAPI UI](assets/asyncapi-ui-screenshot.png)

## Annotation Mental Model

- Put Saunter annotations on the messaging boundary, not the HTTP boundary.
- Annotate producer methods and consumer methods.
- Keep controllers and adapters thin.
- Use `[Message]` only when you need to override inferred message metadata.
- Use class-level annotations only when shared declaration is genuinely clearer than method-level placement.

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
});
```

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
                ["signupMessage"] = new AsyncApiMessageDescriptor("signupMessage", "signupMessage", "Signup event", null, null, "signupPayload", null, null, null, null, null, null, [])
                {
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
            ["routedChannel"] = new AsyncApiChannelDescriptor("routedChannel", "user.signup", null, null, null, null, ["rabbitmq"], ["signupMessage"], [])
            {
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
    document.Middleware.Route = "/asyncapi/fleet/asyncapi.json";
    document.Middleware.UiBaseRoute = "/asyncapi/fleet/ui";
    document.Middleware.UiTitle = "Fleet API";
    document.Document.Asyncapi = "3.0.0";
    document.Document.Info = new AsyncApiInfoDescriptor { Title = "Fleet API", Version = "1.0.0" };
});

services.ConfigureAsyncApiDocument("config", document =>
{
    document.AttributeDocumentName = "v1";
    document.MarkerTypes.Add(typeof(ConfigPublisher));
    document.Middleware.Route = "/asyncapi/config/asyncapi.json";
    document.Middleware.UiBaseRoute = "/asyncapi/config/ui";
    document.Middleware.UiTitle = "Config Messaging API";
    document.Document.Asyncapi = "3.0.0";
    document.Document.Info = new AsyncApiInfoDescriptor { Title = "Config Messaging API", Version = "1.0.0" };
});
```

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
