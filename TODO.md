# TODO

## DX Improvements

### Annotation Defaults And Inference

- [x] Make `[Message]` optional for the common case.
  - If a method already has `[SendOperation(typeof(Foo))]` or `[ReceiveOperation(typeof(Foo))]`, generate the message automatically when no `[Message]` attribute is present.
  - Keep `[Message]` as the override path for custom ids, names, titles, headers, bindings, correlation ids, and external docs.

- [x] Change the default `OperationId` fallback to the member name.
  - Current fallback is verbose: `DeclaringType.Member.action`.
  - Better default: `TurnOn`, `TurnOff`, `ReceiveLightMeasurement`.
  - Keep explicit `OperationId` only for collisions or external naming requirements.

- [x] Infer `channelId` from `address` by default.
  - `ChannelAttribute` currently requires both `channelId` and `address`.
  - In many cases the user only truly knows the address.
  - Add a convention-based default naming strategy so users can write the shorter form and override the id only when needed.

- [x] Infer payload type from method signatures when `SendOperation()` / `ReceiveOperation()` is used without a payload type.
  - MassTransit consumer: infer from `ConsumeContext<T>` / `IConsumer<T>`.
  - Controller or publisher method: infer from a single complex payload parameter when unambiguous.

- [x] Improve default message metadata inference.
  - Keep `messageId` machine-friendly and sanitized.
  - Make `name` stable and logical.
  - Make `title` human-friendly from the CLR type name.

- [x] Formalize convention precedence.
  - Always use: `explicit attribute > inferred convention > fallback default`.
  - Make that precedence rule part of the implementation and documentation.

- [x] Introduce `AsyncApiInferenceOptions`.
  - Candidate toggles:
  - infer operation ids from method names
  - infer channel ids from addresses
  - infer payload type from method signatures
  - infer channel addresses from ASP.NET route metadata
  - auto-fill `defaultContentType`

### Annotation Surface Cleanup

- [x] Reduce stringly-typed references.
  - `BindingsRef`, `Reply`, `CorrelationId`, and server names are all raw strings.
  - Add Roslyn analyzers and/or typed helpers to catch mistakes earlier.

- [x] Revisit `ChannelParameterAttribute(string name, Type type)`.
  - Today `Type` mostly affects enum extraction.
  - Either support richer parameter schema generation or simplify the API so it does not imply more power than it actually has.

- [x] Unify tag DX.
  - Operations/messages use string tags.
  - Channels/servers can carry richer tag objects in descriptors.
  - Choose one consistent authoring story.

- [x] Clarify class-level vs method-level annotations.
  - Method-level annotations should be the default recommendation.
  - Class-level annotations should be documented as the shared/declarative path, not the default.

### Validation And Tooling

- [x] Add Roslyn analyzers for common annotation mistakes.
  - Duplicate operation ids
  - bad reference names
  - malformed external docs URLs
  - channel parameter/address mismatches
  - annotations placed on the wrong member kind

- [x] Improve validation error messages to suggest fixes.
  - Example: “Remove `[ChannelParameter(\"x\")]` or add `{x}` to the address.”
  - Example: “Use an enum type if you want generated enum values.”

### MassTransit Example DX

- [x] Add a truly minimal MassTransit + Saunter example.
  - One contract
  - one publisher
  - one consumer
  - one server
  - minimal explicit metadata

- [x] Keep the current `MassTransitStreetlights` example as the advanced, spec-shaped example.
  - It should not be the first example a new user sees.

- [x] Make the runtime/spec mismatch opt-in instead of the main path.
  - Current `MassTransitStreetlights` example runs on in-memory transport while documenting RabbitMQ.
  - That is acceptable for an advanced example, but not ideal for the primary onboarding story.

- [x] Show the smallest useful annotation set in the MassTransit docs/examples.
  - Prefer examples that use only:
  - `[AsyncApi]`
  - `[Channel(...)]`
  - `[SendOperation(typeof(T))]` or `[ReceiveOperation(typeof(T))]`
  - Add `[Message]` only when overriding defaults.

### Documentation

- [x] Update the docs to lean into the happy path.
  - Show what Saunter infers automatically.
  - Show when explicit annotations are actually required.

- [x] Add a dedicated “annotation mental model” section.
  - Put annotations on the messaging boundary, not the HTTP boundary.
  - Annotate producer methods and consumer methods.
  - Keep controllers/adapters thin.

- [x] Split docs/examples into:
  - minimal getting-started path
  - advanced spec-shaping path

## MassTransit Example Findings

- [x] The current `MassTransitStreetlights` example is structurally good but still too advanced for onboarding.
  - It teaches a spec-shaped workflow before it teaches the happy path.

- [x] The send-side annotation experience in the example is too verbose.
  - Each producer method repeats `Channel`, `ChannelParameter`, `SendOperation`, and `Message`.
  - This makes Saunter look more ceremony-heavy than it needs to.

- [x] The runtime/spec mismatch can undermine trust.
  - The example runs with MassTransit in-memory transport while publishing an AsyncAPI document that models RabbitMQ.

- [x] The example currently has to explain generator gaps inline.
  - `messageTraits`
  - numeric schema bounds
  - channel-local message alias reuse
  - YAML output
  - These comments are accurate, but they make the example feel like a compatibility exercise instead of a clean introduction.

## Follow-Up Tasks

- [x] Fix `MassTransitStreetlights` publisher methods so `streetlightId` is reflected in the published message shape instead of being ignored.
- [x] Make named AsyncAPI route token detection culture-invariant in service registration.
- [x] Remove shared static `NullabilityInfoContext` usage from schema generation so concurrent generation is safe.
- [x] Add focused end-to-end tests for inference precedence across method-level, class-level, and filter-modified documents.
- [x] Decide whether inferred operation ids should preserve method casing exactly or apply a configurable naming strategy.
- [x] Decide whether channel id inference should default to a dotted path, camel case, or a user-configurable strategy.
- [x] Evaluate whether channel parameter generation should grow beyond enum extraction into richer schema support.
- [x] Consider package-level shipping for analyzers so consumers get the diagnostics automatically.
- [x] Add focused tests for disabling individual inference toggles in `AsyncApiInferenceOptions`.
- [x] Decide whether channel tags should also gain richer metadata on the annotation surface or stay string-only by design.
- [x] Add an integration test that snapshots the generated document from `MassTransitMinimal` to lock in the happy-path defaults.

## New Follow-Up Tasks

- [ ] Extend channel parameter modeling to full typed parameter schemas if/when the underlying AsyncAPI model supports more than `default`, `enum`, and `examples`.

## Implementation Priorities

1. Make `[Message]` optional by default in docs and examples.
2. Change the default `OperationId` to the member name.
3. Add `channelId` inference from `address`.
4. Add method-signature payload inference.
5. Add analyzers for string refs and duplicate ids.
6. Add a minimal MassTransit example.
7. Introduce `AsyncApiInferenceOptions`.
