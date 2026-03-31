# MassTransit Streetlights Example

This example is a MassTransit-based adaptation of the Streetlights AsyncAPI example.

It is configured to generate an AsyncAPI 3.0.0 document shaped like `Streetlights.yml`, but with RabbitMQ-style server metadata instead of Kafka.

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
- `Controllers/` contains the HTTP adapter only; it no longer owns messaging annotations
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
