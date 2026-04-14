# AsyncAPI 3.0.0 Compatibility Audit

Source spec:
- https://github.com/asyncapi/spec/blob/v3.0.0/spec/asyncapi.md
- https://raw.githubusercontent.com/asyncapi/spec/v3.0.0/spec/asyncapi.md

Scope:
- This audit reflects the current generator implementation in `src/Saunter`.
- Validation was cross-checked against the current code and existing unit tests in `test/Saunter.Tests`.

## Notable Changes Since The Previous Audit

- Re-validation on 2026-04-14 found a remaining AsyncAPI 3 terminology mismatch in the public authoring surface: `MessageAttribute.MessageId` is still exposed as a first-class property even though AsyncAPI 3.0.0's `Message Object` has no `messageId` field. Saunter interprets that value as the reusable message key in `channels.*.messages` and `components.messages`; output stays structurally valid, but the API naming remains v2-flavored.
  - See [MessageAttribute.cs](src/Saunter/AttributeProvider/Attributes/MessageAttribute.cs), [AttributeMessageResolver.cs](src/Saunter/AttributeProvider/AttributeMessageResolver.cs#L166-L196), [AsyncApiDescriptorMapper.cs](src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L30-L52), and the AsyncAPI 3.0.0 `Message Object` section in the source spec.
- Re-validation on 2026-04-13 found an additional version-enforcement gap: `AsyncApiDocumentWriter` still accepts arbitrary `3.x` version strings such as `3.1.0` and serializes them as AsyncAPI 3.0 instead of rejecting anything other than `3.0.0`.
  - See [AsyncApiDocumentWriter.cs](src/Saunter/SharedKernel/AsyncApiDocumentWriter.cs#L17-L34) and [DocumentWriterTests.cs](test/Saunter.Tests/SharedKernel/DocumentWriterTests.cs#L10-L37).
- `Parameter.default` and `Parameter.examples` are implemented now. `ChannelParameterAttribute` exposes both fields, the channel builder maps them into descriptors, and the mapper projects them into `components.parameters`.
  - See [ChannelParameterAttribute.cs](src/Saunter/AttributeProvider/Attributes/ChannelParameterAttribute.cs#L28-L35), [AttributeChannelBuilder.cs](src/Saunter/AttributeProvider/AttributeChannelBuilder.cs#L109-L127), and [AsyncApiDescriptorMapper.cs](src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L66-L74).
- `Operation.reply.messages` now has a first-class attribute surface. `OperationAttribute` can declare a distinct reply payload type and an optional reply channel address, the document provider registers a dedicated reply message/schema, and the operation builder maps those message ids into `operation.reply.messages`.
  - See [OperationAttribute.cs](src/Saunter/AttributeProvider/Attributes/OperationAttribute.cs), [AttributeMessageResolver.cs](src/Saunter/AttributeProvider/AttributeMessageResolver.cs), [AttributeDocumentProvider.cs](src/Saunter/AttributeProvider/AttributeDocumentProvider.cs), and [AttributeOperationBuilder.cs](src/Saunter/AttributeProvider/AttributeOperationBuilder.cs).
- Channel tags are richer than before. `ChannelTagAttribute` can emit full tag objects with `description` and `externalDocs`; operation and message tags are still name-only.
  - See [ChannelTagAttribute.cs](src/Saunter/AttributeProvider/Attributes/ChannelTagAttribute.cs#L6-L22) and [AttributeChannelBuilder.cs](src/Saunter/AttributeProvider/AttributeChannelBuilder.cs#L33-L52).
- Channel ids are easier to author explicitly now. In addition to the positional `(channelId, address)` constructor, `ChannelAttribute` supports `[Channel("address", ChannelId = "customId")]`, which is useful when inferred channel ids collide even though the AsyncAPI 3 `channels` map requires unique case-sensitive `{channelId}` keys.
  - See [ChannelAttribute.cs](src/Saunter/AttributeProvider/Attributes/ChannelAttribute.cs) and [InferenceBehaviorTests.cs](test/Saunter.Tests/AttributeProvider/DocumentGenerationTests/InferenceBehaviorTests.cs).
- Validation coverage is broader than the previous audit described. The current validator checks unknown channel messages, unknown servers, unknown message/operation/channel binding refs, unknown correlation ids, unknown operation trait refs, unknown operation message refs, unknown reply channels, and unknown reply-channel messages. The channel builder also rejects invalid parameter names, malformed address expressions, and query/fragment-bearing addresses.
  - See [AsyncApiDocumentValidator.cs](src/Saunter/AttributeProvider/AsyncApiDocumentValidator.cs) and [AttributeChannelBuilder.cs](src/Saunter/AttributeProvider/AttributeChannelBuilder.cs#L57-L107).
- AsyncAPI 3.0 serialization now normalizes nullable schemas into `oneOf` + `null` and strips the legacy `nullable` keyword from emitted JSON.
  - See [AsyncApiDocumentWriter.cs](src/Saunter/SharedKernel/AsyncApiDocumentWriter.cs#L17-L118).
- AsyncAPI 3.0 nullable normalization no longer crashes when a nullable usage schema wraps a component reference such as `allOf: [{ $ref: ... }], nullable: true`. The writer now preserves the reference as a pure `$ref`-bearing branch and wraps nullability around it instead of writing scalar metadata through an unresolved reference wrapper.
  - See [AsyncApiDocumentWriter.cs](src/Saunter/SharedKernel/AsyncApiDocumentWriter.cs) and [DocumentWriterTests.cs](test/Saunter.Tests/SharedKernel/DocumentWriterTests.cs).
- String-keyed CLR dictionaries now emit AsyncAPI map schemas via `additionalProperties` instead of reflecting CLR members like `count`, `keys`, or `values`. Unsupported map shapes fail fast with a clear exception.
  - See [AsyncApiSchemaGenerator.cs](src/Saunter/SharedKernel/AsyncApiSchemaGenerator.cs) and [SchemaGeneratorTests.cs](test/Saunter.Tests/SharedKernel/SchemaGeneratorTests.cs).
- Re-validation on 2026-04-14 confirmed that reusable nested schema component ids are now deterministic across a document. Root payload ids stay on their existing inferred names, while newly discovered nested object/array components use CLR-qualified ids so sibling payload graphs can no longer collide on generic names such as `modifier` or `metadata`.
  - See [AsyncApiSchemaGenerator.cs](src/Saunter/SharedKernel/AsyncApiSchemaGenerator.cs), [AttributeMessageResolverTests.cs](test/Saunter.Tests/AttributeProvider/UnitTests/AttributeMessageResolverTests.cs), and [AsyncApiTypesTests.cs](test/Saunter.Tests/AttributeProvider/DocumentProviderTests/AsyncApiTypesTests.cs).
- Nullable root payloads no longer lose their payload schema reference. The message resolver now registers a reusable nullable wrapper component when the root usage schema has no id.
  - See [AttributeMessageResolver.cs](src/Saunter/AttributeProvider/AttributeMessageResolver.cs) and [AttributeMessageResolverTests.cs](test/Saunter.Tests/AttributeProvider/UnitTests/AttributeMessageResolverTests.cs).
- Re-validation against the AsyncAPI 3.0.0 spec found one still-open compliance gap that was not called out explicitly in the previous audit: the generator still honors prototype `Asyncapi` values that start with `2.`.
  - See [AttributeDocumentProvider.cs](src/Saunter/AttributeProvider/AttributeDocumentProvider.cs#L45-L56).
- Re-validation against the AsyncAPI 3.0.0 spec also found that the root `servers`, `channels`, and `operations` maps are modeled as concrete descriptors only. The spec allows `Reference Object` entries in all three maps, but Saunter's top-level descriptor surface currently does not.
  - See [AsyncApiDocumentDescriptor.cs](src/Saunter/Descriptors/AsyncApiDocumentDescriptor.cs) and the AsyncAPI 3.0.0 spec sections for `Servers Object`, `Channels Object`, and `Operations Object`.
- Error reporting for document generation conflicts is more actionable now. Channel/address conflicts, parameter conflicts, duplicate operation ids, duplicate message/schema descriptors, and several channel/header inference failures now include the conflicting ids and a summary of the competing definitions instead of generic failure text.
  - See [AsyncApiChannelUnion.cs](src/Saunter/SharedKernel/AsyncApiChannelUnion.cs), [AttributeDocumentProvider.cs](src/Saunter/AttributeProvider/AttributeDocumentProvider.cs), [AttributeMessageResolver.cs](src/Saunter/AttributeProvider/AttributeMessageResolver.cs), and [AttributeChannelBuilder.cs](src/Saunter/AttributeProvider/AttributeChannelBuilder.cs).

## Summary Table

| Status | Areas |
|---|---|
| Supported | Root `asyncapi`, `id`, `defaultContentType`; root `info.title`, `info.version`, `info.description`, `info.contact`, `info.license`, `info.termsOfService`; server `host`, `description`, `protocol`, `protocolVersion`, `variables`, `security`, `tags`; server variable `default`, `description`, `enum`, `examples`; channel `address` (string form), `messages`, `title`, `summary`, `description`, `servers`, `parameters`, `tags`, `bindings`; operation `action`, `channel`, `title`, `summary`, `description`, `bindings`, `messages`, `reply.channel`, `reply.address`; message `headers`, `payload`, `correlationId`, `contentType`, `name`, `title`, `summary`, `description`, `externalDocs`, `bindings`; parameter `enum`, `default`, `description`, `examples`, `location`; components `schemas`, `messages`, `parameters`, `correlationIds`, `securitySchemes`, `operationBindings`, `messageBindings`, `channelBindings`, `operationTraits`; schema primitives, objects, arrays, enums, refs, `required`, `items`, `additionalProperties`, `oneOf`, `allOf`, nullable output normalization, nullable root wrapper components |
| Partially Supported | Root `info`; root `servers`; root `channels`; root `operations`; root `components`; server object overall; channel `address` null case; channel object overall; operation `traits`; operation object overall; operation tags as name-only tags; message tags as name-only tags; message object overall; schema object overall; validation coverage |
| Missing | `info.tags`, `info.externalDocs`; server `pathname`, `title`, `summary`, `externalDocs`, `bindings`; channel `externalDocs`; operation `security`, `externalDocs`; message `examples`, `traits`; components `channels`, `operations`, `servers`, `serverVariables`, `replies`, `replyAddresses`, `externalDocs`, `tags`, `messageTraits`, `serverBindings`; Multi Format Schema authoring surface; YAML output; most advanced JSON Schema keywords |
| Incorrect / Risky | `defaultContentType` is still auto-injected as `application/json`; the document provider still accepts prototype `Asyncapi` values beginning with `2.` instead of enforcing 3.0.0-only output; the writer still accepts arbitrary `3.x` values instead of enforcing `3.0.0`; public authoring still exposes `MessageAttribute.MessageId` even though AsyncAPI 3 `Message Object` has no `messageId` field; operation `security` is emitted as an empty list rather than intentionally authored; reusable component maps only expose concrete descriptors, not general reference-object authoring; null-address channels are awkward to author intentionally; many AsyncAPI invariants still are not validated |

## Compatibility Matrix

| Area | Spec 3.0.0 status | Current support | Notes |
|---|---|---|---|
| Root `asyncapi` | Required | Supported | Forced to `3.0.0` unless the source document explicitly targets `2.x`; see [AttributeDocumentProvider.cs](src/Saunter/AttributeProvider/AttributeDocumentProvider.cs#L53-L55) |
| AsyncAPI version enforcement in writer | Required for strict 3.0.0 compliance | Partially supported | [AsyncApiDocumentWriter.cs](src/Saunter/SharedKernel/AsyncApiDocumentWriter.cs#L20-L26) treats any `3.x` version string as AsyncAPI 3.0 and tests currently exercise `3.1.0` successfully |
| Root `id` | Optional | Supported | Present in [AsyncApiDocumentDescriptor.cs](src/Saunter/Descriptors/AsyncApiDocumentDescriptor.cs#L5-L22) |
| Root `info` | Required | Partially supported | Supports core metadata only; `info.tags` and `info.externalDocs` are missing |
| Root `servers` | Optional | Partially supported | Concrete server objects are supported, but the descriptor surface does not allow `Reference Object` entries even though the spec does |
| Root `defaultContentType` | Optional | Supported with opinionated default | Auto-set to `application/json` when inference is enabled |
| Root `channels` | Optional | Partially supported | Generated and merged in [AttributeDocumentProvider.cs](src/Saunter/AttributeProvider/AttributeDocumentProvider.cs#L65-L79); channel ids can be explicit or inferred, but the root map cannot contain `Reference Object` entries |
| Root `operations` | Required by app model | Partially supported | Generated in [AttributeDocumentProvider.cs](src/Saunter/AttributeProvider/AttributeDocumentProvider.cs#L65-L79), but the root map cannot contain `Reference Object` entries |
| Root `components` | Optional | Partially supported | Only a subset of component maps is modeled in [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28) |
| Root `info.title` / `version` / `description` / `contact` / `license` / `termsOfService` | Mixed | Supported | Mapped by [AsyncApiDocumentMapper.cs](src/Saunter/SharedKernel/AsyncApiDocumentMapper.cs#L61-L80) |
| Root `info.tags` / `info.externalDocs` | Optional | Missing | No descriptor fields in [AsyncApiInfoDescriptor.cs](src/Saunter/Descriptors/AsyncApiInfoDescriptor.cs#L5-L35) |
| Server `host` | Required | Supported | In [AsyncApiServerDescriptor.cs](src/Saunter/Descriptors/AsyncApiServerDescriptor.cs#L6-L21) |
| Server `description` | Optional | Supported | In [AsyncApiServerDescriptor.cs](src/Saunter/Descriptors/AsyncApiServerDescriptor.cs#L6-L21) |
| Server `protocol` | Required | Supported | In [AsyncApiServerDescriptor.cs](src/Saunter/Descriptors/AsyncApiServerDescriptor.cs#L6-L21) |
| Server `protocolVersion` | Optional | Supported | In [AsyncApiServerDescriptor.cs](src/Saunter/Descriptors/AsyncApiServerDescriptor.cs#L6-L21) |
| Server `pathname` | Optional | Missing | Not modeled |
| Server `title` / `summary` | Optional | Missing | Not modeled |
| Server `variables` | Optional | Supported | Variable descriptor includes `default`, `description`, `enum`, and `examples` |
| Server `security` | Optional | Supported | Concrete security schemes only on the descriptor surface |
| Server `tags` | Optional | Supported | Stored as tag objects on the descriptor |
| Server `externalDocs` | Optional | Missing | Not modeled |
| Server `bindings` | Optional | Missing | Neither server-level bindings nor `components.serverBindings` are modeled |
| Server variable `default` / `description` / `enum` / `examples` | Optional | Supported | See [AsyncApiServerDescriptor.cs](src/Saunter/Descriptors/AsyncApiServerDescriptor.cs#L23-L32) |
| Channel `address` | Optional (`string \| null`) | Partially supported | String addresses are supported; there is no clean authored null-address path |
| Channel `messages` | Optional | Supported | Emitted in [AsyncApiDescriptorMapper.cs](src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L77-L93) |
| Channel `title` / `summary` / `description` | Optional | Supported | Via [ChannelAttribute.cs](src/Saunter/AttributeProvider/Attributes/ChannelAttribute.cs#L8-L73) |
| Channel `servers` | Optional | Supported | Server refs are emitted and unknown refs are validated |
| Channel `parameters` | Conditional | Supported | Includes `enum`, `default`, `description`, `examples`, and `location` |
| Channel `tags` | Optional | Supported | Rich tag objects are supported through `ChannelTagAttribute`; plain string tags also work |
| Channel `externalDocs` | Optional | Missing | Not modeled |
| Channel `bindings` | Optional | Supported | Via `BindingsRef` |
| Operation `action` | Required | Supported | In [OperationAttribute.cs](src/Saunter/AttributeProvider/Attributes/OperationAttribute.cs#L7-L75) |
| Operation `channel` | Required | Supported | Always emitted as a channel ref |
| Operation `title` / `summary` / `description` | Optional | Supported | Built in [AttributeOperationBuilder.cs](src/Saunter/AttributeProvider/AttributeOperationBuilder.cs#L13-L25) |
| Operation `security` | Optional | Missing as authored surface | Mapper emits an empty list instead of a modeled value |
| Operation `tags` | Optional | Partially supported | Name-only tags; no first-class rich tag or ref surface |
| Operation `externalDocs` | Optional | Missing | Not modeled |
| Operation `bindings` | Optional | Supported | Via `BindingsRef` |
| Operation `traits` | Optional | Partially supported | Trait refs can exist on descriptors and be validated, but there is no first-class attribute property |
| Operation `messages` | Optional | Supported | Emitted as refs to channel messages |
| Operation `reply.channel` | Optional | Supported | Via `Reply` on [OperationAttribute.cs](src/Saunter/AttributeProvider/Attributes/OperationAttribute.cs#L24-L33) |
| Operation `reply.address` | Optional | Supported | Via `ReplyAddressLocation` / `ReplyAddressDescription` |
| Operation `reply.messages` | Optional | Supported | Distinct reply payloads can be authored through `ReplyMessagePayloadType`; the provider can auto-create a reply channel for message-only replies (`Reply` plus reply message ids), for dynamic replies (`reply.address`), or when `ReplyChannelAddress` is supplied |
| Message `headers` | Optional | Supported with validation | Headers schema must be object-like |
| Message `payload` | Optional | Supported | Generated from CLR types |
| Message `correlationId` | Optional | Supported | Reference-based surface via `MessageAttribute.CorrelationId` |
| Message `contentType` | Optional | Supported | Via `MessageAttribute.ContentType` |
| Message `name` / `title` / `summary` / `description` | Optional | Supported | Via [MessageAttribute.cs](src/Saunter/AttributeProvider/Attributes/MessageAttribute.cs#L20-L90) |
| Message authoring key (`MessageAttribute.MessageId`) | Not a Message Object field in AsyncAPI 3.0.0 | Risky terminology | Saunter uses it as the reusable message map key, not as a serialized Message Object property |
| Message `tags` | Optional | Partially supported | Name-only tags; no rich tag object or ref surface |
| Message `externalDocs` | Optional | Supported | Via `MessageAttribute.ExternalDocs` |
| Message `bindings` | Optional | Supported | Via `BindingsRef` |
| Message `examples` | Optional | Missing | Mapper initializes an empty list |
| Message `traits` | Optional | Missing | Mapper initializes an empty list |
| Parameter `enum` | Optional | Supported | Enum values inferred from enum-typed channel parameters |
| Parameter `default` | Optional | Supported | Via `ChannelParameterAttribute.DefaultValue` |
| Parameter `description` | Optional | Supported | Via `ChannelParameterAttribute.Description` |
| Parameter `examples` | Optional | Supported | Via `ChannelParameterAttribute.Examples` |
| Parameter `location` | Optional | Supported | Via `ChannelParameterAttribute.Location` |
| Components `schemas` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28) |
| Components `messages` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28) |
| Components `parameters` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28) |
| Components `correlationIds` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28) |
| Components `securitySchemes` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28) |
| Components `operationBindings` / `messageBindings` / `channelBindings` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28) |
| Components `operationTraits` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28) |
| Components `channels` / `operations` / `servers` | Optional | Missing | Not modeled as reusable component maps |
| Components `serverVariables` | Optional | Missing | Not modeled |
| Components `replies` / `replyAddresses` | Optional | Missing | Not modeled |
| Components `externalDocs` / `tags` | Optional | Missing | Not modeled |
| Components `messageTraits` | Optional | Missing | Not modeled |
| Components `serverBindings` | Optional | Missing | Not modeled |
| Schema primitives / objects / arrays / enums / refs | Core | Supported | Generated in [AsyncApiSchemaGenerator.cs](src/Saunter/SharedKernel/AsyncApiSchemaGenerator.cs#L14-L512) |
| Schema `required`, `items`, `additionalProperties`, `oneOf`, `allOf` | Core subset | Supported | Mapped in [AsyncApiSchemaMapper.cs](src/Saunter/SharedKernel/AsyncApiSchemaMapper.cs#L10-L55) |
| Schema nullability | Core subset | Supported with normalization | Serialized for AsyncAPI 3 as `oneOf` + `null`, without the legacy `nullable` keyword |
| Rich JSON Schema keywords | Optional but important | Missing | No support for keywords like `pattern`, numeric bounds, schema `default`, schema `examples`, etc. |
| Multi Format Schema Object | Supported by spec | Missing in authored surface | Saunter only exposes its JSON-schema-oriented descriptor path |
| YAML output | Allowed by spec | Missing | Writer exposes JSON output only |
| Validation coverage | N/A | Partial | Validates several reference relationships and address constraints, but not the full set of AsyncAPI invariants |

## Detailed Findings

### Confirmed Support

- Root document generation still forces AsyncAPI 3 output unless the prototype explicitly starts with `2.`. It also auto-populates `defaultContentType` with `application/json` when inference is enabled.
  - See [AttributeDocumentProvider.cs](src/Saunter/AttributeProvider/AttributeDocumentProvider.cs#L53-L59).
- Root document mapping still covers the implemented top-level fields only: `id`, `asyncapi`, `defaultContentType`, `info`, `components`, `servers`, `channels`, and `operations`.
  - See [AsyncApiDocumentDescriptor.cs](src/Saunter/Descriptors/AsyncApiDocumentDescriptor.cs#L5-L22) and [AsyncApiDocumentMapper.cs](src/Saunter/SharedKernel/AsyncApiDocumentMapper.cs#L20-L58).
- Server support includes `description`, which the previous audit summary omitted, and server variables support `examples`.
  - See [AsyncApiServerDescriptor.cs](src/Saunter/Descriptors/AsyncApiServerDescriptor.cs#L6-L32) and [AsyncApiDocumentMapper.cs](src/Saunter/SharedKernel/AsyncApiDocumentMapper.cs#L83-L113).
- Channel parameter support is stronger than the previous audit stated: `enum`, `default`, `examples`, `description`, and `location` are all carried through to the generated component parameter.
  - See [AsyncApiParameterDescriptor.cs](src/Saunter/AttributeProvider/Descriptors/AsyncApiParameterDescriptor.cs#L5-L11) and [AsyncApiDescriptorMapperTests.cs](test/Saunter.Tests/AttributeProvider/UnitTests/AsyncApiDescriptorMapperTests.cs#L79-L100).
- Channels Object support includes both explicit and inferred `channelId` authoring. That maps cleanly onto the spec's case-sensitive `{channelId}` patterned field and now has a named-property escape hatch for address patterns that sanitize to the same inferred id.
  - See [ChannelAttribute.cs](src/Saunter/AttributeProvider/Attributes/ChannelAttribute.cs) and [InferenceBehaviorTests.cs](test/Saunter.Tests/AttributeProvider/DocumentGenerationTests/InferenceBehaviorTests.cs).
- Reply metadata is supported end-to-end for `reply.channel`, `reply.address`, and `reply.messages`, including distinct reply payloads and generated reply channels from attribute metadata.
  - See [AttributeOperationBuilderTests.cs](test/Saunter.Tests/AttributeProvider/UnitTests/AttributeOperationBuilderTests.cs), [MethodAttributesTests.cs](test/Saunter.Tests/AttributeProvider/DocumentGenerationTests/MethodAttributesTests.cs), and [AsyncApiDescriptorMapperTests.cs](test/Saunter.Tests/AttributeProvider/UnitTests/AsyncApiDescriptorMapperTests.cs#L16-L77).
- String-keyed CLR dictionaries are now modeled as AsyncAPI maps. The schema generator produces `type: object` with `additionalProperties`, and the schema mapper carries that through to the serialized schema model.
  - See [AsyncApiSchemaGenerator.cs](src/Saunter/SharedKernel/AsyncApiSchemaGenerator.cs), [AsyncApiSchemaMapper.cs](src/Saunter/SharedKernel/AsyncApiSchemaMapper.cs), and [SchemaGeneratorTests.cs](test/Saunter.Tests/SharedKernel/SchemaGeneratorTests.cs).
- Nested reusable object schemas now get stable document-wide component ids even when multiple payload roots contain different CLR types with the same simple name. The generator keeps root payload ids unchanged, reuses assigned ids for recursive/self references, and emits CLR-qualified ids for newly discovered nested reusable components.
  - See [AsyncApiSchemaGenerator.cs](src/Saunter/SharedKernel/AsyncApiSchemaGenerator.cs), [AttributeMessageResolverTests.cs](test/Saunter.Tests/AttributeProvider/UnitTests/AttributeMessageResolverTests.cs), and [AsyncApiTypesTests.cs](test/Saunter.Tests/AttributeProvider/DocumentProviderTests/AsyncApiTypesTests.cs).
- Nullable root payload schemas now keep a reusable component id. When the generator returns a nullable usage wrapper with `Root.Id == null`, the message resolver registers a wrapper component such as `int32Nullable` and uses that as `PayloadSchemaId`.
  - See [AttributeMessageResolver.cs](src/Saunter/AttributeProvider/AttributeMessageResolver.cs) and [AttributeMessageResolverTests.cs](test/Saunter.Tests/AttributeProvider/UnitTests/AttributeMessageResolverTests.cs).
- AsyncAPI 3 nullable component-reference usages are now safe to serialize. A schema property shaped like `allOf: [{ $ref: "#/components/schemas/foo" }], nullable: true` is rewritten to `oneOf: [{ allOf: [{ $ref: ... }] }, { type: "null" }]` without mutating the referenced component schema or crashing on unresolved ByteBard reference wrappers.
  - See [AsyncApiDocumentWriter.cs](src/Saunter/SharedKernel/AsyncApiDocumentWriter.cs) and [DocumentWriterTests.cs](test/Saunter.Tests/SharedKernel/DocumentWriterTests.cs).

### Partial Support And Gaps

- `Info Object` remains partial. Core metadata is mapped, but `info.tags` and `info.externalDocs` still have no descriptor fields and therefore no normal authoring surface.
  - See [AsyncApiInfoDescriptor.cs](src/Saunter/Descriptors/AsyncApiInfoDescriptor.cs#L5-L35) and [AsyncApiDocumentMapper.cs](src/Saunter/SharedKernel/AsyncApiDocumentMapper.cs#L61-L80).
- `Components Object` remains partial. The descriptor only exposes `schemas`, `messages`, `parameters`, `correlationIds`, `securitySchemes`, `operationBindings`, `messageBindings`, `channelBindings`, and `operationTraits`.
  - See [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28).
- The top-level `servers`, `channels`, and `operations` maps are also only partially modeled. AsyncAPI 3.0.0 allows each entry in those maps to be either a concrete object or a `Reference Object`, but [AsyncApiDocumentDescriptor.cs](src/Saunter/Descriptors/AsyncApiDocumentDescriptor.cs) only exposes concrete descriptor dictionaries.
  - See the AsyncAPI 3.0.0 spec sections for `Servers Object`, `Channels Object`, and `Operations Object`.
- Channel `address` is supported only as a normal string path. The spec allows `string | null`, but the attribute-based surface does not provide a clean way to intentionally author a null address.
  - See [ChannelAttribute.cs](src/Saunter/AttributeProvider/Attributes/ChannelAttribute.cs#L8-L73).
- Operation traits are supported only indirectly. The descriptor can carry trait references and the validator enforces that they exist, but there is no first-class operation attribute property for traits.
  - See [AsyncApiOperationDescriptor.cs](src/Saunter/AttributeProvider/Descriptors/AsyncApiOperationDescriptor.cs#L7-L21) and [AsyncApiDocumentValidator.cs](src/Saunter/AttributeProvider/AsyncApiDocumentValidator.cs#L70-L76).
- Message and operation tags are still modeled as simple strings, even though the spec tag list can contain full tag objects or references. Channel tags are the only place with a richer built-in surface.
  - See [MessageAttribute.cs](src/Saunter/AttributeProvider/Attributes/MessageAttribute.cs#L87-L90), [OperationAttribute.cs](src/Saunter/AttributeProvider/Attributes/OperationAttribute.cs#L24-L33), and [AsyncApiDescriptorMapper.cs](src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L46-L47).
- Schema support is still a deliberate subset. The generator and mapper cover the common structural cases, but not the broader AsyncAPI 3 / JSON Schema surface.
  - See [AsyncApiSchemaDescriptor.cs](src/Saunter/SharedKernel/Descriptors/AsyncApiSchemaDescriptor.cs#L15-L38), [AsyncApiSchemaGenerator.cs](src/Saunter/SharedKernel/AsyncApiSchemaGenerator.cs#L14-L512), and [AsyncApiSchemaMapper.cs](src/Saunter/SharedKernel/AsyncApiSchemaMapper.cs#L10-L55).
- Map support is intentionally limited to string-keyed dictionary shapes. Non-string keys and non-generic `IDictionary` implementations now fail fast instead of serializing an invalid reflected-object schema.
  - See [AsyncApiSchemaGenerator.cs](src/Saunter/SharedKernel/AsyncApiSchemaGenerator.cs).

### Missing Surface

- Server fields still missing from the descriptor and mapper: `pathname`, `title`, `summary`, `externalDocs`, and `bindings`.
  - See [AsyncApiServerDescriptor.cs](src/Saunter/Descriptors/AsyncApiServerDescriptor.cs#L6-L32) and [AsyncApiDocumentMapper.cs](src/Saunter/SharedKernel/AsyncApiDocumentMapper.cs#L83-L113).
- Channel `externalDocs` is still missing from the attribute descriptor surface.
  - See [AsyncApiChannelDescriptor.cs](src/Saunter/AttributeProvider/Descriptors/AsyncApiChannelDescriptor.cs#L7-L23).
- Operation `security` and `externalDocs` are still missing from the descriptor surface.
  - See [AsyncApiOperationDescriptor.cs](src/Saunter/AttributeProvider/Descriptors/AsyncApiOperationDescriptor.cs#L7-L21) and [OperationAttribute.cs](src/Saunter/AttributeProvider/Attributes/OperationAttribute.cs#L7-L33).
- Message `examples` and `traits` are still not authorable through the built-in descriptor path; the mapper initializes both to empty collections.
  - See [AsyncApiDescriptorMapper.cs](src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L36-L52).
- Reusable component maps still missing: `channels`, `operations`, `servers`, `serverVariables`, `replies`, `replyAddresses`, `externalDocs`, `tags`, `messageTraits`, and `serverBindings`.
  - See [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28).
- Multi Format Schema authoring is still missing from Saunter's descriptor surface. The implementation generates only AsyncAPI JSON-schema-shaped output.
  - See [AsyncApiSchemaDescriptor.cs](src/Saunter/SharedKernel/Descriptors/AsyncApiSchemaDescriptor.cs#L15-L38).
- YAML output is still absent from the writer surface.
  - See [AsyncApiDocumentWriter.cs](src/Saunter/SharedKernel/AsyncApiDocumentWriter.cs#L17-L34).

### Incorrect / Risky Behavior

- The generator still treats `defaultContentType` as an inferred default of `application/json`, which is convenient but opinionated relative to the spec's optional field.
  - See [AttributeDocumentProvider.cs](src/Saunter/AttributeProvider/AttributeDocumentProvider.cs#L56-L59).
- The document provider is still not 3.0.0-only. If the prototype document sets `Asyncapi` to a value beginning with `2.`, `GetDocument` preserves that version instead of forcing `3.0.0`, even though the current project intent is to generate AsyncAPI 3 documents.
  - See [AttributeDocumentProvider.cs](src/Saunter/AttributeProvider/AttributeDocumentProvider.cs#L53-L55).
- The document writer is also not 3.0.0-only. `WriteJson` treats any version beginning with `3.` as AsyncAPI 3.0, so descriptors authored with `Asyncapi = "3.1.0"` are accepted and serialized instead of being rejected for strict 3.0.0 output.
  - See [AsyncApiDocumentWriter.cs](src/Saunter/SharedKernel/AsyncApiDocumentWriter.cs#L20-L26) and [DocumentWriterTests.cs](test/Saunter.Tests/SharedKernel/DocumentWriterTests.cs#L10-L37).
- The public authoring API still exposes `MessageAttribute.MessageId`, even though the AsyncAPI 3.0.0 `Message Object` does not define a `messageId` field. Saunter currently treats that property as the reusable message key for `channels.*.messages` and `components.messages`, so the emitted JSON remains valid but the public terminology is misleading.
  - See [MessageAttribute.cs](src/Saunter/AttributeProvider/Attributes/MessageAttribute.cs), [AttributeMessageResolver.cs](src/Saunter/AttributeProvider/AttributeMessageResolver.cs#L166-L196), and [AsyncApiDescriptorMapper.cs](src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L30-L52).
- Operation `security` is not modeled. The mapper currently emits `Security = new List<AsyncApiSecurityScheme>()`, which means generated AsyncAPI 3 operations serialize an empty security array rather than an intentionally authored value or omission.
  - See [AsyncApiDescriptorMapper.cs](src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L146-L162).
- For the supported reusable component maps, the descriptor surface still only exposes concrete objects, not the broader `Object | Reference Object` authoring model the spec allows.
  - See [AsyncApiComponentsDescriptor.cs](src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs#L9-L28).
- The validator now enforces the reply-address/channel invariant from AsyncAPI 3.0.0. When `reply.address` is present, the referenced reply channel must have `address: null` or no address; reply-message membership is also validated against that reply channel.
  - See [AsyncApiDocumentValidator.cs](src/Saunter/AttributeProvider/AsyncApiDocumentValidator.cs) and the AsyncAPI 3.0.0 spec `Operation Reply Object` section.
- Validation is meaningfully better than before, but it is still narrow compared with the spec. There is no broad validation of required field presence, URL formats outside the few factory helpers, component key regexes across all maps, or deeper reference and schema invariants.
  - See [AsyncApiDocumentValidator.cs](src/Saunter/AttributeProvider/AsyncApiDocumentValidator.cs#L7-L88).
