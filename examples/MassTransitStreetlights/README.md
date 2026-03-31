# MassTransit Streetlights Example

This example is a MassTransit-based adaptation of the Streetlights AsyncAPI example.

It is configured to generate an AsyncAPI 3.0.0 document shaped like `Streetlights.yml`, but with RabbitMQ-style server metadata instead of Kafka.

This is a spec-shaped example, not the minimal getting-started path. It is useful if you want to see how Saunter fits into a realistic MassTransit project structure while still aiming at a specific AsyncAPI document shape.

If you want the smallest possible MassTransit + Saunter setup, start with `examples/MassTransitMinimal` first and come back to this one afterward.

## Start Here

If you are reading this example to understand the developer experience, open the files in this order:

1. `Program.cs`
2. `Configuration/MassTransitServiceCollectionExtensions.cs`
3. `Producers/StreetlightCommandPublisher.cs`
4. `Consumers/LightMeasuredConsumer.cs`
5. `AsyncApi/StreetlightsAsyncApiDocument.cs`

That path shows the main mental model first, before the Streetlights-specific shaping details.

## Mental Model

- Put Saunter annotations on the messaging boundary, not the HTTP boundary.
- Annotate producer methods that publish/send messages.
- Annotate consumer methods that receive messages.
- Keep controllers thin. In this example, the controller only adapts HTTP requests into calls to the publisher service.
- Keep message contracts in `Contracts/` and transport/spec setup in `Configuration/` and `AsyncApi/`.

## What Matches The Target

- Streetlights domain
- four channels
- one receive operation and three send operations
- one RabbitMQ server
- channel parameters and message metadata

## Project Layout

- `Contracts/` contains the bus message contracts and shared AsyncAPI header model
- `Producers/` contains the MassTransit publishing boundary and carries the send-side AsyncAPI annotations
- `Consumers/` contains the receive-side MassTransit consumer and consumer definition
- `Controllers/` contains the HTTP adapter only; it intentionally does not own messaging annotations
- `AsyncApi/` and `Configuration/` contain the spec document and service registration

## Known Gaps

The current Saunter generator cannot reproduce the target YAML exactly yet. The main remaining gaps are:

- `messageTraits` are not currently modeled as reusable components
- schema keywords such as `minimum` and `maximum` are not emitted
- channel-local message aliases that point to the same component message are not supported
- JSON output is supported by the middleware; YAML is not currently emitted

## Running

```bash
cd ~/saunter/src/Saunter.UI
npm install

cd ~/saunter/examples/MassTransitStreetlights
dotnet run
```

Open:

- `http://localhost:5000/asyncapi/asyncapi.json`
- `http://localhost:5000/asyncapi/ui/`

## Notes

The example runs on MassTransit's in-memory transport so it starts locally without external infrastructure. The generated AsyncAPI document still describes the target RabbitMQ topology, so the runtime transport and published spec are intentionally different.

If you want the simplest possible MassTransit + Saunter setup, this example currently goes further than that. The extra document metadata and target-shaping choices are here to stay close to `Streetlights.yml`, not because they are the minimum required annotations.
