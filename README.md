# Saunter

![CI](https://github.com/asyncapi/saunter/workflows/CI/badge.svg)
[![NuGet Badge](https://buildstats.info/nuget/saunter?includePreReleases=true)](https://www.nuget.org/packages/Saunter/)

Saunter is a code-first [AsyncAPI](https://github.com/asyncapi/asyncapi) documentation generator for .NET.

## Getting Started

Start with one of these examples:

- [examples/MassTransitMinimal](https://github.com/asyncapi/saunter/tree/main/examples/MassTransitMinimal) for the happy path and inferred defaults
- [examples/MassTransitStreetlights](https://github.com/asyncapi/saunter/tree/main/examples/MassTransitStreetlights) for the advanced, spec-shaped MassTransit example
- [examples/StreetlightsAPI](https://github.com/asyncapi/saunter/tree/main/examples/StreetlightsAPI) for the non-MassTransit Streetlights sample

1. Install the Saunter package.

   ```bash
   dotnet add package Saunter
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
   - message id, name, and title from the payload type

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
   app.UseEndpoints(endpoints =>
   {
       endpoints.MapAsyncApiDocuments();
       endpoints.MapAsyncApiUi();
       endpoints.MapControllers();
   });
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
       "streetlights.measurement": {
         "address": "subscribe/light/measured"
       }
     },
     "operations": {
       "ReceiveLightMeasurement": {
         "action": "receive",
         "channel": {
           "$ref": "#/channels/streetlights.measurement"
         }
       }
     }
   }
   ```

7. Open the UI.

   ![AsyncAPI UI](https://raw.githubusercontent.com/asyncapi/saunter/main/assets/asyncapi-ui-screenshot.png)

## Annotation Mental Model

- Put Saunter annotations on the messaging boundary, not the HTTP boundary.
- Annotate producer methods and consumer methods.
- Keep controllers and adapters thin.
- Use `[Message]` only when you need to override inferred message metadata.
- Use class-level annotations only when shared declaration is genuinely clearer than method-level placement.

## Configuration

See [the options source code](https://github.com/asyncapi/saunter/blob/main/src/Saunter/AsyncApiOptions.cs) for detailed info.

```csharp
services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = new[] { typeof(Startup) };
    options.AddAsyncApiChannelFilter<MyAsyncApiChannelFilter>();
    options.AddOperationFilter<MyOperationFilter>();
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

- inferred operation ids preserve the member name casing by default
- inferred channel ids use the configured `ChannelIdGenerator`
- richer channel tag metadata can be declared with `[ChannelTag(...)]`
- channel parameters can now carry `DefaultValue` and `Examples`
- Saunter packages ship the built-in analyzers automatically

## Bindings

Bindings can be referenced from `ChannelAttribute` and `OperationAttribute` through `BindingsRef`.

```csharp
using ByteBard.AsyncAPI.Bindings.AMQP;
using ByteBard.AsyncAPI.Bindings.Http;
using ByteBard.AsyncAPI.Models;

services.AddAsyncApiSchemaGeneration(options =>
{
    options.AsyncApi = new AsyncApiDocumentDescriptor
    {
        Components =
        {
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
        }
    };
});
```

## Multiple AsyncAPI Documents

```csharp
services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = new[] { typeof(FooMessageBus) };
});

services.ConfigureNamedAsyncApi("Foo", asyncApi =>
{
    asyncApi.Asyncapi = "3.0.0";
    asyncApi.Info = new AsyncApiInfoDescriptor { Title = "Foo API", Version = "1.0.0" };
});
```

## Breaking Changes

- LEGO AsyncAPI.NET was replaced with `ByteBard.AsyncAPI.NET`, `ByteBard.AsyncAPI.NET.Readers`, and `ByteBard.AsyncAPI.NET.Bindings`.
- Public API types now use Saunter descriptors, including `AsyncApiOptions.AsyncApi`, `IAsyncApiDocumentProvider`, and the filter interfaces.
- `PublishOperationAttribute` and `SubscribeOperationAttribute` were removed and replaced with `SendOperationAttribute` and `ReceiveOperationAttribute`.
- `ChannelAttribute` still supports `(channelId, address)`, supports inferred ids from a single address in the happy path, and lets you override the inferred id with `[Channel("address", ChannelId = "customId")]`.
- Generated documents now use AsyncAPI v3 root `operations` and v3 channel `address` fields instead of v2 channel-local `publish` and `subscribe`.
- Existing code that mutates or asserts v2 document shape must be updated to the v3 document model.

Migration examples:

```csharp
[Channel("temperature.sensor", "sensors/temperature")]
[SendOperation(typeof(TemperatureReading))]
public void PublishTemperature(TemperatureReading reading) { }
```

```csharp
[Channel("temperature.sensor", "sensors/temperature")]
[ReceiveOperation(typeof(TemperatureReading))]
public void ConsumeTemperature(TemperatureReading reading) { }
```

## Contributing

See our [contributing guide](https://github.com/asyncapi/saunter/blob/main/CONTRIBUTING.md).

## Thanks

- This project is heavily inspired by [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore).
- We use [ByteBard AsyncAPI.NET](https://github.com/ByteBardOrg/AsyncAPI.NET) for schema modeling and serialization.
