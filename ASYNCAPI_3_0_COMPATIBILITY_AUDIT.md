# AsyncAPI 3.0.0 Compatibility Audit

Source spec:
- https://raw.githubusercontent.com/asyncapi/website/refs/heads/master/markdown/docs/reference/specification/v3.0.0.md

This audit reflects the current generator implementation in `src/Saunter`.

## Summary Table

| Status | Areas |
|---|---|
| Supported | Root `asyncapi`, `id`, `servers`, `defaultContentType`, `channels`, `operations`; server `host`, `protocol`, `protocolVersion`, `variables`, `security`, `tags`; channel `messages`, `title`, `summary`, `description`, `servers`, `parameters`, `tags`, `bindings`; operation `action`, `channel`, `title`, `summary`, `description`, `bindings`, `messages`, `reply.channel`, `reply.address`; message `headers`, `payload`, `correlationId`, `contentType`, `name`, `title`, `summary`, `description`, `tags`, `externalDocs`, `bindings`; parameter `enum`, `description`, `location`; components `schemas`, `messages`, `parameters`, `correlationIds`, `securitySchemes`, `operationBindings`, `messageBindings`, `channelBindings`, `operationTraits`; schema primitives, objects, arrays, enums, refs, `required`, `items`, `oneOf`, `allOf`, `nullable` |
| Partially Supported | Root `info`; root `components`; channel `address`; operation `traits`; operation `security`; message tags as name-only tags; server object overall; channel object overall; operation object overall; message object overall; parameter object overall; schema object overall |
| Missing | `info.tags`, `info.externalDocs`; server `pathname`, `title`, `summary`, `externalDocs`, `bindings`; channel `externalDocs`; operation `externalDocs`, `reply.messages`; message `examples`, `traits`; parameter `default`, `examples`; components `channels`, `operations`, `servers`, `serverVariables`, `replies`, `replyAddresses`, `externalDocs`, `tags`, `messageTraits`, `serverBindings`; Multi Format Schema authoring surface; YAML output; most advanced JSON Schema keywords |
| Incorrect / Risky | Channel null-address use cases are awkward because normal attribute flow always expects a string address; operation `security` is emitted as an empty list rather than intentionally modeled; validation only covers a narrow subset of spec invariants; `defaultContentType` is opinionatedly injected as `application/json`; root/channel/component maps do not expose general reference-object authoring surfaces |

| Area | Spec 3.0.0 status | Current support | Notes |
|---|---|---|---|
| Root `asyncapi` | Required | Supported | Forced to `3.0.0` in [AttributeDocumentProvider.cs:52](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeDocumentProvider.cs#L52) |
| Root `id` | Optional | Supported | Present in [AsyncApiDocumentDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiDocumentDescriptor.cs) |
| Root `info` | Required | Partially supported | `title/version/description/contact/license/termsOfService` supported; `info.tags` and `info.externalDocs` missing in [AsyncApiInfoDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiInfoDescriptor.cs) |
| Root `servers` | Optional | Supported | Mapped in [AsyncApiDocumentMapper.cs](/home/raido/dev/community/saunter/src/Saunter/SharedKernel/AsyncApiDocumentMapper.cs) |
| Root `defaultContentType` | Optional | Supported | Defaulted to `application/json` in [AttributeDocumentProvider.cs:55](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeDocumentProvider.cs#L55) |
| Root `channels` | Optional | Supported | Generated and merged in [AttributeDocumentProvider.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeDocumentProvider.cs) |
| Root `operations` | Required by app model | Supported | Generated in [AttributeDocumentProvider.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeDocumentProvider.cs) |
| Root `components` | Optional | Partially supported | Only a subset of component maps implemented in [AsyncApiComponentsDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs) |
| Server `host` | Required | Supported | In [AsyncApiServerDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiServerDescriptor.cs) |
| Server `protocol` | Required | Supported | In [AsyncApiServerDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiServerDescriptor.cs) |
| Server `protocolVersion` | Optional | Supported | In [AsyncApiServerDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiServerDescriptor.cs) |
| Server `pathname` | Optional | Missing | No field in [AsyncApiServerDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiServerDescriptor.cs) |
| Server `title` / `summary` | Optional | Missing | Not modeled |
| Server `variables` | Optional | Supported | In [AsyncApiServerDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiServerDescriptor.cs) |
| Server `security` | Optional | Supported | In [AsyncApiServerDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiServerDescriptor.cs) |
| Server `tags` | Optional | Supported | In [AsyncApiServerDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiServerDescriptor.cs) |
| Server `externalDocs` | Optional | Missing | Not modeled |
| Server `bindings` | Optional | Missing at server object level | Only `components.serverBindings` is also missing |
| Channel `address` | Optional (`string \| null`) | Partially supported | String supported via [ChannelAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/ChannelAttribute.cs); no normal null-address path |
| Channel `messages` | Optional | Supported | Generated in [AsyncApiDescriptorMapper.cs:80](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L80) |
| Channel `title` / `summary` / `description` | Optional | Supported | Via [ChannelAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/ChannelAttribute.cs) |
| Channel `servers` | Optional | Supported | Via [ChannelAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/ChannelAttribute.cs) and validated in [AsyncApiDocumentValidator.cs:9](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDocumentValidator.cs#L9) |
| Channel `parameters` | Conditional | Supported well | Built and validated in [AttributeChannelBuilder.cs:30](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeChannelBuilder.cs#L30) |
| Channel `tags` | Optional | Supported | `AsyncApiTag` list on [AsyncApiChannelDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Descriptors/AsyncApiChannelDescriptor.cs) |
| Channel `externalDocs` | Optional | Missing | Not modeled |
| Channel `bindings` | Optional | Supported | `BindingsRef` in [ChannelAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/ChannelAttribute.cs) |
| Operation `action` | Required | Supported | In [OperationAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/OperationAttribute.cs) |
| Operation `channel` | Required | Supported | Always emitted in [AsyncApiDescriptorMapper.cs:97](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L97) |
| Operation `title` / `summary` / `description` | Optional | Supported | In [AttributeOperationBuilder.cs:15](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeOperationBuilder.cs#L15) |
| Operation `security` | Optional | Missing as authored surface | Mapper emits empty list in [AsyncApiDescriptorMapper.cs:105](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L105) |
| Operation `tags` | Optional | Supported | String tags only |
| Operation `externalDocs` | Optional | Missing | Not modeled |
| Operation `bindings` | Optional | Supported | `BindingsRef` in [OperationAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/OperationAttribute.cs) |
| Operation `traits` | Optional | Partially supported | Component trait refs supported via filters and [OperationTraitsTests.cs](/home/raido/dev/community/saunter/test/Saunter.Tests/AttributeProvider/OperationTraitsTests.cs); no first-class attribute surface |
| Operation `messages` | Optional | Supported | Emitted as refs under channel messages in [AsyncApiDescriptorMapper.cs:101](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L101) |
| Operation `reply.channel` | Optional | Supported | In [AttributeOperationBuilder.cs:27](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeOperationBuilder.cs#L27) |
| Operation `reply.address` | Optional | Supported | In [AsyncApiDescriptorMapper.cs:152](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L152) |
| Operation `reply.messages` | Optional | Missing | Always empty in [AsyncApiDescriptorMapper.cs:159](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L159) |
| Message `headers` | Optional | Supported with validation | Must be object-like, enforced in [AttributeMessageResolver.cs:174](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeMessageResolver.cs#L174) |
| Message `payload` | Optional | Supported | Generated from CLR types in [AttributeMessageResolver.cs:149](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeMessageResolver.cs#L149) |
| Message `correlationId` | Optional | Supported | Ref-only surface via [MessageAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/MessageAttribute.cs) |
| Message `contentType` | Optional | Supported | Via [MessageAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/MessageAttribute.cs) |
| Message `name` / `title` / `summary` / `description` | Optional | Supported | Via [MessageAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/MessageAttribute.cs) |
| Message `tags` | Optional | Supported | String tags only |
| Message `externalDocs` | Optional | Supported | Via [MessageAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/MessageAttribute.cs) |
| Message `bindings` | Optional | Supported | `BindingsRef` supported |
| Message `examples` | Optional | Missing | Mapper emits empty list in [AsyncApiDescriptorMapper.cs:48](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L48) |
| Message `traits` | Optional | Missing | Mapper emits empty list in [AsyncApiDescriptorMapper.cs:49](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDescriptorMapper.cs#L49) |
| Parameter `enum` | Optional | Supported | In [AttributeChannelBuilder.cs:82](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AttributeChannelBuilder.cs#L82) |
| Parameter `default` | Optional | Missing | Not modeled |
| Parameter `description` | Optional | Supported | In [ChannelParameterAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/ChannelParameterAttribute.cs) |
| Parameter `examples` | Optional | Missing | Not modeled; mapper emits empty examples list |
| Parameter `location` | Optional | Supported | In [ChannelParameterAttribute.cs](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/Attributes/ChannelParameterAttribute.cs) |
| Components `schemas` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs) |
| Components `messages` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs) |
| Components `parameters` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs) |
| Components `correlationIds` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs) |
| Components `securitySchemes` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs) |
| Components `operationBindings` / `messageBindings` / `channelBindings` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs) |
| Components `operationTraits` | Optional | Supported | In [AsyncApiComponentsDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/Descriptors/AsyncApiComponentsDescriptor.cs) |
| Components `channels` / `operations` / `servers` | Optional | Missing | Not modeled as reusable component maps |
| Components `serverVariables` | Optional | Missing | Not modeled as reusable component map |
| Components `replies` / `replyAddresses` | Optional | Missing | Not modeled |
| Components `externalDocs` / `tags` | Optional | Missing | Not modeled as reusable components |
| Components `messageTraits` | Optional | Missing | Not modeled |
| Components `serverBindings` | Optional | Missing | Not modeled |
| Schema Object basics | Broad spec surface | Partially supported | Core subset only in [AsyncApiSchemaDescriptor.cs](/home/raido/dev/community/saunter/src/Saunter/SharedKernel/Descriptors/AsyncApiSchemaDescriptor.cs) |
| Schema primitives/objects/arrays/enums/refs | Core | Supported | In [AsyncApiSchemaGenerator.cs:56](/home/raido/dev/community/saunter/src/Saunter/SharedKernel/AsyncApiSchemaGenerator.cs#L56) |
| Schema `required`, `items`, `oneOf`, `allOf`, `nullable` | Core subset | Supported | In [AsyncApiSchemaMapper.cs:25](/home/raido/dev/community/saunter/src/Saunter/SharedKernel/AsyncApiSchemaMapper.cs#L25) |
| Rich JSON Schema keywords | Optional but important | Missing | No support for pattern, min/max, default, examples, additionalProperties, etc. |
| Multi Format Schema Object | Supported by spec | Missing in authored surface | Only JSON-schema wrapper path exists |
| YAML output | Allowed by spec | Missing | JSON only in [AsyncApiDocumentWriter.cs](/home/raido/dev/community/saunter/src/Saunter/SharedKernel/AsyncApiDocumentWriter.cs) |
| Validation coverage | N/A | Partial | Unknown server refs and reply channel refs validated in [AsyncApiDocumentValidator.cs:7](/home/raido/dev/community/saunter/src/Saunter/AttributeProvider/AsyncApiDocumentValidator.cs#L7); many required-field and ref invariants still unchecked |
