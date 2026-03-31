# MassTransit Minimal

This is the MassTransit + Saunter happy-path example.

It intentionally uses the smallest useful annotation set:

- `[AsyncApi]`
- `[Channel("orders.submitted")]`
- `[SendOperation]`
- `[ReceiveOperation]`

What is inferred automatically:

- channel id from the channel address
- operation id from the method name
- payload type from the method signature
- message id, name, and title from the payload type

## Run

```bash
cd ~/saunter/src/Saunter.UI
npm install

cd ~/saunter/examples/MassTransitMinimal
dotnet run
```

Open:

- `http://localhost:5001/asyncapi/asyncapi.json`
- `http://localhost:5001/asyncapi/ui/`

## Read This Example In Order

1. `Program.cs`
2. `Producers/OrderSubmittedPublisher.cs`
3. `Consumers/OrderSubmittedConsumer.cs`
4. `Contracts/OrderSubmitted.cs`

If you want the advanced, spec-shaped example instead, see `examples/MassTransitStreetlights`.
