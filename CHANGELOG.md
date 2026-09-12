# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)

<!-- Please update the links section at the bottom when adding a new version. -->
## Unreleased

## [v1.6.0]
### Added
- Generated payload schemas preserve descriptions, string and array length annotations, and numeric ranges. Property constraints stay local to each usage, including nullable properties; byte-array bounds describe the base64 wire representation. Numeric ranges support inclusive int and double bounds; unsupported typed string or exclusive bounds fail explicitly.

## [v1.5.2]
### Added
- Abstract and interface payloads with string-tagged `JsonDerivedType` alternatives export disjoint `oneOf` schemas that preserve inherited properties, nullable references, and recursive references. Unsupported polymorphic shapes fail explicitly.

## [v1.5.1]
### Fixed
- `System.Object` payload members, including `Dictionary<string, object>` values, now generate an unconstrained JSON Schema instead of `type: object`. System.Text.Json writes such members as whatever JSON the runtime value serializes to, so a string or number there was previously rejected by consumers that validate against the document.

## [v1.5.0]
### Added
- `MapAsyncApiUi(uiBaseRoute, documentUrl, title)` overload serves the AsyncAPI UI for a
  document the host serves itself — typically one generated at build time and returned as
  static bytes. It requires no `ConfigureAsyncApiDocument` registration and maps no document
  endpoint, so it composes with a host's own document routes instead of competing with them.
- `DeriveYamlRoute` is public so hosts serving their own documents can derive the YAML
  sibling route the same way this library does.

### Fixed
- `System.Text.Json.JsonElement` payload properties now generate an unconstrained JSON Schema instead of exposing the CLR-only `valueKind` property.

## [v1.4.0]
### Changed
- The document-generation pipeline is now usable from external (non-web-host) generators: `IAsyncApiSchemaGenerator`/`AsyncApiSchemaGenerator`, `GeneratedSchemaDescriptors`, and `IAsyncApiDocumentValidator`/`AsyncApiDocumentValidator` are public. Consumers can construct `AsyncApiDocumentDescriptor` programmatically, generate payload schemas from CLR types, and validate referential integrity without assembly scanning or DI.

## [v1.3.1]
### Fixed
- Generated enum schemas now honor `[JsonStringEnumMemberName]` values before the legacy `[EnumMember]` fallback, keeping AsyncAPI contracts aligned with System.Text.Json wire values.

## [v1.3.0]
### Added
- Repeatable `[ReplyMessage]` annotations describe multiple success/error reply variants with independent message metadata and payload schema ids.
- `OperationAttribute.ReplyMessagePayloadSchemaId` provides a schema-id override for the backward-compatible single-reply authoring surface.

### Fixed
- Dynamically addressed reply channels no longer inherit physical bindings from the request channel.
- Repeated nested collection types with different generic nullability no longer collide on a shared generated component id.
- Recursive dictionary graphs retain any component required by their generated `$ref` values.
- Repeatable reply annotations are scoped to the single reply-enabled operation on mixed-operation members.
- Reply alternatives with identical payload, headers, content type, and bindings validation are rejected.
- Blank explicit reply message ids are rejected instead of falling back to a generic component key.
- Reply analyzer diagnostics now match runtime validation for empty reply ids and legacy single-reply metadata.

## [v1.2.2]
### Added
- `MessageAttribute.PayloadSchemaId` overrides the payload's schema key in components/schemas, resolving the "conflicting schema definitions" error when two payload types share the same simple name (e.g. a legacy and a V1 contract) and renaming the CLR type isn't an option. All `$ref`s to the renamed schema are rewritten, including self-references in recursive types.

## [v1.2.1]
### Fixed
- `AddAsyncApiSchemaGeneration` now registers logging itself, so `IAsyncApiDocumentProvider` resolves from a bare `ServiceCollection` without host logging (regression introduced in 1.2.0 by the document provider's new `ILogger` dependency).

## [v1.2.0]
### Added
- Entry-assembly scanning by default: when `AsyncApiOptions.AssemblyMarkerTypes` is empty, the application's entry assembly is scanned for `[AsyncApi]` types, so the minimal setup needs no marker configuration.
- `AsyncApiDocumentDescriptor.Asyncapi` defaults to `"3.0.0"`, removing the boilerplate every consumer wrote.
- Generated documents default `info.title`/`info.version` from the document's scan assembly (name and informational version) when not configured, so a document without explicit `Info` is still spec-valid.
- `AsyncApiMiddlewareOptions.UiTitle` falls back to the document's `info.title` when not set.
- `AsyncApiServerDescriptor.FromUri`/`FromConnectionString` build a server descriptor from a broker connection URI (`rabbitmq://…` maps to protocol `amqp`), replacing hand-rolled conversion helpers.
- `MapAsyncApi()` maps the document endpoints and the UI in one call.
- Filter instances can now be registered directly: `AddDocumentFilter(IDocumentFilter)`, `AddChannelFilter(IChannelFilter)`, `AddOperationFilter(IOperationFilter)`.
- Opt-in MassTransit consumer auto-discovery (`AsyncApiOptions.Discovery.DiscoverMassTransitConsumers`): unannotated `IConsumer<T>` implementations are documented with a receive operation per consumed message type. The channel address defaults to the message type's `Namespace:TypeName` (MassTransit MessageUrn form) and is customizable via `MassTransitChannelAddressGenerator`; annotated consumers are skipped so attribute authoring always wins. Reflection-only — no MassTransit package dependency.
- A warning is logged when a generated document contains no channels and no operations, naming the scanned assemblies and the attribute document name that was matched (the previous behavior was a silent empty document).
- Mapped document, YAML, and UI routes are logged at startup.
- When the embedded UI assets are missing (source build without `npm install`), the UI now serves an explanatory page linking to the document route instead of a blank page.

### Changed
- **Breaking:** Saunter now targets .NET 10 only; the net8.0 and net9.0 targets were dropped. Tests and examples were moved to net10.0 as well.
- **Breaking:** requesting a named document that is neither configured nor matched by any `[AsyncApi("name")]` attribute now throws a descriptive `InvalidOperationException` instead of silently serving the default document prototype.
- **Breaking:** `AsyncApiMiddlewareOptions.UiTitle` is now `string?` (default `null`); the effective title resolves to `info.title`, then `"AsyncAPI"`.
- **Breaking:** `AsyncApiOptions.DocumentFilters`/`ChannelFilters`/`OperationFilters` changed from `IEnumerable<Type>` to `IReadOnlyList<FilterDescriptor>` to support instance registration.
- Documents that previously serialized an empty (spec-invalid) `info` object now serialize a generated one.

## [v1.1.0]
### Added
- Every `.json` document route now serves a `.yaml` sibling (e.g. `/asyncapi/asyncapi.yaml`) via the new `IAsyncApiDocumentWriter.WriteYaml`.
- `AsyncApiOptions.ValidateOnStartup`: registered documents are generated once at startup so misconfiguration fails fast instead of returning a 500 on first request. Defaults to on in the Development environment.
- Analyzer rule SAUN007 flags invalid operation reply configurations at build time; all analyzer rules now have help links backed by `docs/analyzers.md`.
- `AsyncApiOptions.AddChannelFilter`, matching the `AddDocumentFilter`/`AddOperationFilter` naming.
- Parameterless constructors on `AsyncApiMessageDescriptor`, `AsyncApiChannelDescriptor`, and `AsyncApiOperationDescriptor` for object-initializer construction.
- `examples/MultiDocument` covering multi-document hosting, `TypeFilter`, filters, `PropertyNameSelector`, and custom inference generators.
- XML doc comments on the entire public API surface; CS1591 is no longer suppressed.
- `MapAsyncApiUi` logs a warning when the embedded UI assets are missing (source build without `npm install` in `src/Saunter.UI`).

### Changed
- `IChannelFilter`, `IOperationFilter`, `ChannelFilterContext`, and `DocumentFilterContext` moved from the global namespace into `Saunter.Options.Filters`.
- `ConfigureNamedAsyncApi` and `AddAsyncApiChannelFilter` are marked obsolete in favor of `ConfigureAsyncApiDocument` and `AddChannelFilter`; both keep working.
- `IAsyncApiDocumentWriter` gained `WriteYaml` (breaking for custom implementations).
- `AsyncApiOptions.NamedApis` is now get-only (breaking if it was reassigned rather than populated).

## [v0.20.0]
### Changed
- AsyncAPI generation now targets AsyncAPI 3.0.0 with the new descriptor-first document model.
- Schema generation, nullability handling, reply mapping, and validation were tightened for the AsyncAPI 3 migration.
- Saunter now targets .NET 8 and .NET 9 and the examples/docs were updated to match the new model.
- See [PR #1](https://github.com/APL-Digital/saunter/pull/1) for the full migration context.

## [v0.14.0] - ?
### Changed
- [Change AsyncApi data structure to LEGO AsyncAPI.NET](https://github.com/m-wild/saunter/issues/188)
- [Replace NJsonSchema with own implementation](https://github.com/m-wild/saunter/issues/188)
- [Allow usages of the annotation attributes on interfaces](https://github.com/m-wild/saunter/issues/213)
- Bump ws from 7.5.3 to 7.5.10 in /src/Saunter.UI

## [v0.13.0] - 2024-01-16
### Changed
- [Updated NJsonSchema to v11.0.0](https://github.com/m-wild/saunter/issues/179)
- [Document names are correctly copied to clones when using named docs](https://github.com/m-wild/saunter/issues/172)
- Updated @asyncapi/react-component to v1.2.11
- Use npm-ci instead of npm-install in ci scripts

## [v0.12.0] - 2023-06-15
### Added
- [Add support for message headers](https://github.com/tehmantra/saunter/issues/150)
### Changed
- Updated @asyncapi/react-component to v1.0.0-next.48
- Updated NJsonSchema dependencies ([fixes compatibility with NSwag](https://github.com/tehmantra/saunter/issues/156))
### Fixed
- [Duplicated operation when types of the same assembly are used in AssemblyMarkerTypes](https://github.com/tehmantra/saunter/issues/163)
- [Tags do not work without description and external docs](https://github.com/tehmantra/saunter/issues/149)
  - Updates Tag Description and ExternalDocs to ignore nulls to eliminate async api parser errors.
  - Adds support for Tags from a class that was missing.

## [v0.11.0] - 2022-10-03
### Added
- Message and Operation attributes now allow setting tags

## [v0.10.0] - 2022-08-22
### Changed
- AsyncAPI spec version bumped to 2.4.0
- Added messageId to Message and MessageAttribute 
- Updated @asyncapi/react-component to v1.0.0-next.40
- Target net6.0 only - multitargeting was painful to test & maintain
- Updated NJsonSchema to v10.7.2
- [Add README to NuGet package #110](https://github.com/tehmantra/saunter/issues/110)

### Fixed
- [Saunter breaks when you try register two same channels #133](https://github.com/tehmantra/saunter/issues/133)
- [NJsonSchema uses unsupported Json Schema. #138](https://github.com/tehmantra/saunter/issues/138)
- [Use NJsonSchema for the JSON Schema implementation #60](https://github.com/tehmantra/saunter/issues/60)
- [Error: Maximum call stack size exceeded #123](https://github.com/tehmantra/saunter/issues/123)

## [v0.9.1] - 2021-11-08
### Fixed
- Hosting behind a reverse proxy now works correctly. See tests/Saunter.IntegrationTests.ReverseProxy/README.md for an example.

## [v0.9.0] - 2021-10-17
### Changed
- AsyncAPI spec version bumped to 2.2.0
  - New optional property `string[] Servers` available on `ChannelAttribute`
  - New optional property `List<string> Servers` available on `ChannelItem`
- Bump UI library to v1.0.0-next.21

## [v0.8.0] - 2021-09-11
### Changed
- Filters are now registered as types (e.g. `options.AddOperationFilter<T>`) and resolved from an `IServiceProvider`

## [v0.7.1] - 2021-08-04
### Fixed
- Include XML documentation file in build (HOW HAD I NOT NOTICED THIS BEFORE?!)

## [v0.7.0] - 2021-08-04
### Changed
- AsyncAPI spec version bumped to 2.1.0
- `Message.Examples` type changed from `IList<IDictionary<string, object>>` to `IList<MessageExample>` to allow specifying the new `name` and `summary` fields.
- New security schemes added.

## [v0.6.0] - 2021-08-03
### Added
- Added ability to generate multiple AsyncAPI documents using the `ConfigureNamedAsyncApi` extension


## [v0.5.0] - 2021-07-28
### Changed
- Added ability to use bindings by specifying a BindingsRef in the attribute e.g. `[Channel("light.measured", BindingsRef = "my-amqp-binding")]`
- Added MQTT bindings
- Swapped custom JSON schema generation for NJsonSchema
- Removed direct dependencies on System.Text.Json



## [v0.4.0] - 2021-07-27
### Changed
- UI no longer proxies to playground.asyncapi.io, but instead uses the asyncapi standalone react component as an embedded resource.
- Removed settings related to the proxied UI
    - AsyncApi.Middleware.UiRoute
    - AsyncApi.Middleware.PlaygroundBaseAddress
- Bumped project dependencies 
```
System.Text.Json 5.0.0 -> 5.0.2
Namotion.Reflection 1.0.14 -> 1.0.23
Microsoft.NET.Test.Sdk 16.8.3 -> 16.10.0
```


## [v0.3.1] - 2021-07-19
### Fixed
- DateTimeOffset causes recursive schema - now treated same as DateTime
- DictionaryKeyToStringConverter to support lists #94

## [v0.3.0] - 2021-07-19
### Changed
- Replaced dependency on Newtonsoft.Json with System.Text.Json
    - Previously we were inspecting types for the attributes provided by Newtonsoft.Json such as `[JsonProperty]`. If you were relying on these attributes, you will now need to set `options.SchemaIdSelector` and/or `options.PropertyNameSelector` to a function which inspects those attributes.
    e.g.
    ```csharp
    services.AddAsyncApiSchemaGeneration(options =>
    {
        options.PropertyNameSelector = prop => 
            prop.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? prop.Name;
    });
    ```

### Added
- Multi-targeting `netcoreapp3.1`, `net5.0`  and `netstandard2.0`
- Async API UI
- Endpoint-aware middleware on `netcoreapp3.1`+
    ```csharp
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapAsyncApiDocuments();
        endpoints.MapAsyncApiUi();
    })
    ```
- Bindings for HTTP, Kafka & RabbitMQ
- Support for Subscribe and Publish operations of the same Channel in different classes
- Code-first discriminator support with `[Discriminator]` and `[DiscriminatorSubType]` attributes
- Support for `[Required]` attribute
- Support for Channel Parameters
- Support for `oneOf` message types (use multiple `[Message(Type)]` attributes)

### Fixed
- `AsyncApiOptions.PropertyNameSelector` was not being used


## [v0.2.0] - 2020-08-04
### Added
- Support System.Guid as a JSON Schema string with "uuid" format.
- Default Schema ID factory handles generics in a human-friendly format 
    - `List<Foo>` becomes `"listOfFoo"`
    - `Dictionary<string, Foo>` becomes `"dictionaryOfStringAndFoo"`
    - etc

## [v0.1.0] - 2020-07-02
### Changed
- First stable release!


<!--
When updating here set baseVersion to the previous tag and targetVersion to your new tag
This link will be dead until after you have completed the pull request and tagged the new version in master
-->

[v1.6.0]: https://github.com/APL-Digital/saunter/compare/v1.5.2...v1.6.0
[v1.5.2]: https://github.com/APL-Digital/saunter/compare/v1.5.1...v1.5.2

[v1.5.1]: https://github.com/APL-Digital/saunter/compare/v1.5.0...v1.5.1
[v1.5.0]: https://github.com/APL-Digital/saunter/compare/v1.4.0...v1.5.0
[v1.4.0]: https://github.com/APL-Digital/saunter/compare/v1.3.1...v1.4.0
[v1.3.1]: https://github.com/APL-Digital/saunter/compare/v1.3.0...v1.3.1
[v1.3.0]: https://github.com/APL-Digital/saunter/compare/v1.2.2...v1.3.0
[v1.2.2]: https://github.com/APL-Digital/saunter/compare/v1.2.1...v1.2.2
[v1.2.1]: https://github.com/APL-Digital/saunter/compare/v1.2.0...v1.2.1
[v1.2.0]: https://github.com/APL-Digital/saunter/compare/v1.1.0...v1.2.0
[v1.1.0]: https://github.com/APL-Digital/saunter/compare/v1.0.8...v1.1.0
[v0.20.0]: https://github.com/APL-Digital/saunter/compare/v0.14.0...v0.20.0
[v0.14.0]: https://github.com/m-wild/saunter/compare/v0.13.0...v0.14.0
[v0.13.0]: https://github.com/m-wild/saunter/compare/v0.12.0...v0.13.0
[v0.12.0]: https://github.com/tehmantra/saunter/compare/v0.11.0...v0.12.0
[v0.11.0]: https://github.com/tehmantra/saunter/compare/v0.10.0...v0.11.0
[v0.10.0]: https://github.com/tehmantra/saunter/compare/v0.9.1...v0.10.0
[v0.9.1]: https://github.com/tehmantra/saunter/compare/v0.9.0...v0.9.1
[v0.9.0]: https://github.com/tehmantra/saunter/compare/v0.8.0...v0.9.0
[v0.8.0]: https://github.com/tehmantra/saunter/compare/v0.7.1...v0.8.0
[v0.7.1]: https://github.com/tehmantra/saunter/compare/v0.7.0...v0.7.1
[v0.7.0]: https://github.com/tehmantra/saunter/compare/v0.6.0...v0.7.0
[v0.6.0]: https://github.com/tehmantra/saunter/compare/v0.5.0...v0.6.0
[v0.5.0]: https://github.com/tehmantra/saunter/compare/v0.4.0...v0.5.0
[v0.4.0]: https://github.com/tehmantra/saunter/compare/v0.3.1...v0.4.0
[v0.3.1]: https://github.com/tehmantra/saunter/compare/v0.3.0...v0.3.1
[v0.3.0]: https://github.com/tehmantra/saunter/compare/v0.2.0...v0.3.0
[v0.2.0]: https://github.com/tehmantra/saunter/compare/v0.1.0...v0.2.0
[v0.1.0]: https://github.com/tehmantra/saunter/compare/97abfdb20e11dccfe4c6b9317e6a7e1fa419fd5c...v0.1.0
