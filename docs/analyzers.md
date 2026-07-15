# Saunter Analyzer Rules

The `Apollo.Saunter` package ships Roslyn analyzers that flag common annotation
mistakes at build time. All rules are in the `Usage` category with `Warning`
severity and are enabled by default.

## SAUN001

**Duplicate AsyncAPI operation id**

Two or more `[SendOperation]`/`[ReceiveOperation]` attributes declare the same
literal `OperationId`. AsyncAPI operation ids are the keys of the document's
root `operations` map, so they must be unique per document.

Fix: use a unique `OperationId`, or remove the explicit id and rely on
member-name inference (each annotated method then contributes its own name).

Note: this rule only sees literal `OperationId` values. Collisions between
*inferred* operation ids (e.g. two overloads of the same method name) are
reported at document-generation time instead.

## SAUN002

**Invalid AsyncAPI external docs URL**

The `ExternalDocs` value on a `[Message]` attribute is not an absolute URI.

Fix: use a fully qualified URL such as `https://example.com/docs/my-message`.

## SAUN003

**Channel parameter does not match address**

A `[ChannelParameter("name")]` declares a parameter that does not appear as a
`{name}` expression in the `[Channel]` address.

Fix: remove the parameter, or add `{name}` to the channel address.

## SAUN004

**Invalid AsyncAPI reference name**

A reference-valued property (`OperationId`, `BindingsRef`, `Reply`,
`CorrelationId`, `MessageId`, `Servers`) contains characters that are not
valid in an AsyncAPI component or server name.

Fix: use only letters, digits, `.`, `-`, or `_`.

## SAUN005

**Annotation is missing surrounding AsyncAPI context**

An annotation was found without the companion annotation it needs to take
effect:

- `[Message]` without a `[SendOperation]` or `[ReceiveOperation]` on the
  method or containing type.
- `[ChannelParameter]` without a `[Channel]` on the method or containing type.

Fix: add the missing companion attribute, or remove the orphaned annotation.

## SAUN006

**Invalid AsyncAPI channel parameter name**

A `[ChannelParameter]` name contains characters that are not valid in an
AsyncAPI channel parameter name.

Fix: use only letters, digits, `-`, or `_`.

## SAUN007

**Invalid AsyncAPI operation reply configuration**

The reply-related properties on `[SendOperation]`/`[ReceiveOperation]` are
combined in a way that document generation rejects:

- `ReplyChannelAddress` and `ReplyAddressLocation` are mutually exclusive —
  a reply channel is either explicitly addressed or dynamically addressed.
- `ReplyChannelAddress`, `ReplyAddressLocation`, and `ReplyMessagePayloadType`
  each require `Reply` to be set to the reply channel id.

Fix: set `Reply` to the reply channel id and keep at most one of
`ReplyChannelAddress`/`ReplyAddressLocation`.
