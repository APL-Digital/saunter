# Saunter

![CI](https://github.com/asyncapi/saunter/workflows/CI/badge.svg)
[![NuGet Badge](https://buildstats.info/nuget/saunter?includePreReleases=true)](https://www.nuget.org/packages/Saunter/)

Saunter is a code-first [AsyncAPI](https://github.com/asyncapi/asyncapi) documentation generator for .NET.

## Getting Started

See [examples/StreetlightsAPI](https://github.com/asyncapi/saunter/tree/main/examples/StreetlightsAPI).

1. Install the Saunter package.

   ```bash
   dotnet add package Saunter
   ```

2. Configure Saunter in `ConfigureServices`.

   ```csharp
   using ByteBard.AsyncAPI.Bindings.AMQP;
   using ByteBard.AsyncAPI.Bindings.Http;
   using ByteBard.AsyncAPI.Models;

   services.AddAsyncApiSchemaGeneration(options =>
   {
       options.AssemblyMarkerTypes = new[] { typeof(StreetlightMessageBus) };
       options.Middleware.UiTitle = "Streetlights API";

       options.AsyncApi = new AsyncApiDocument
       {
           Asyncapi = "3.0.0",
           Info = new AsyncApiInfo
           {
               Title = "Streetlights API",
               Version = "1.0.0",
               Description = "The Smartylighting Streetlights API allows you to remotely manage the city lights.",
               License = new AsyncApiLicense
               {
                   Name = "Apache 2.0",
                   Url = new("https://www.apache.org/licenses/LICENSE-2.0"),
               }
           },
           Servers =
           {
               ["mosquitto"] = new AsyncApiServer { Host = "test.mosquitto.org", Protocol = "mqtt" },
               ["webapi"] = new AsyncApiServer { Host = "localhost:5000", Protocol = "http" },
           },
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

3. Annotate classes or methods with the v3 attributes.

   ```csharp
   using Saunter.AttributeProvider.Attributes;

   [AsyncApi]
   public class StreetlightMessageBus : IStreetlightMessageBus
   {
       [Channel("streetlights.measurement", "subscribe/light/measured", BindingsRef = "amqpDev")]
       [ReceiveOperation(typeof(LightMeasuredEvent), "Light", Summary = "Subscribe to environmental lighting conditions for a particular streetlight.", BindingsRef = "postBind")]
       public void PublishLightMeasurement(LightMeasuredEvent lightMeasuredEvent) { }
   }
   ```

4. Map the endpoints.

   ```csharp
   app.UseEndpoints(endpoints =>
   {
       endpoints.MapAsyncApiDocuments();
       endpoints.MapAsyncApiUi();
       endpoints.MapControllers();
   });
   ```

5. Open the JSON document.

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
       "PublishLightMeasurement.receive": {
         "action": "receive"
       }
     }
   }
   ```

6. Open the UI.

   ![AsyncAPI UI](https://raw.githubusercontent.com/asyncapi/saunter/main/assets/asyncapi-ui-screenshot.png)

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
});
```

## Bindings

Bindings can be referenced from `ChannelAttribute` and `OperationAttribute` through `BindingsRef`.

```csharp
using ByteBard.AsyncAPI.Bindings.AMQP;
using ByteBard.AsyncAPI.Bindings.Http;
using ByteBard.AsyncAPI.Models;

services.AddAsyncApiSchemaGeneration(options =>
{
    options.AsyncApi = new AsyncApiDocument
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
    asyncApi.Info = new AsyncApiInfo { Title = "Foo API", Version = "1.0.0" };
});
```

## Breaking Changes

- LEGO AsyncAPI.NET was replaced with `ByteBard.AsyncAPI.NET`, `ByteBard.AsyncAPI.NET.Readers`, and `ByteBard.AsyncAPI.NET.Bindings`.
- Public API types now use ByteBard models, including `AsyncApiOptions.AsyncApi`, `IAsyncApiDocumentProvider`, and the filter interfaces.
- `PublishOperationAttribute` and `SubscribeOperationAttribute` were removed and replaced with `SendOperationAttribute` and `ReceiveOperationAttribute`.
- `ChannelAttribute` now takes `(channelId, address)` instead of a single channel name.
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
