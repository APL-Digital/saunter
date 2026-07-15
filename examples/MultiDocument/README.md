# MultiDocument

This example demonstrates the configuration surface that the other examples
don't cover:

- `ConfigureAsyncApiDocument(...)` hosting two independent documents
  (`fleet` and `config`) from one process
- `TypeFilter` splitting a single attribute document name (`[AsyncApi("v1")]`)
  across those hosted documents
- `AddDocumentFilter` (adds a heartbeat channel/operation no attribute declares)
- `AddAsyncApiChannelFilter` (tags every channel)
- `PropertyNameSelector` honoring `[JsonPropertyName]`
- a custom `Inference.OperationIdGenerator`

## Run

From the repository root:

```bash
npm install --prefix src/Saunter.UI

dotnet run --project examples/MultiDocument
```

Open:

- `http://localhost:5003/asyncapi/fleet/asyncapi.json`
- `http://localhost:5003/asyncapi/fleet/ui`
- `http://localhost:5003/asyncapi/config/asyncapi.json`
- `http://localhost:5003/asyncapi/config/ui`

## Read This Example In Order

1. `Program.cs`
2. `Fleet/FleetPublisher.cs`
3. `Config/ConfigPublisher.cs`
4. `Filters/HeartbeatDocumentFilter.cs`
5. `Filters/EnvironmentTagChannelFilter.cs`
