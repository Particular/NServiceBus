# Message payloads are not attached to spans

**Date:** 2026-06-10
**Pull request:** [#7798](https://github.com/Particular/NServiceBus/pull/7798), commits dd3a9dbe1 (added) and b08362985 (removed)
**Related:**

- [#7800](https://github.com/Particular/NServiceBus/pull/7800) trimming warning approval
- [#7929](https://github.com/Particular/NServiceBus/pull/7929) trimming and NativeAOT strategy

## Context

Early on the `otel` branch an option `MessagePayloadAsTag` (`None`, `IncomingMessage`,
`OutgoingMessage`, `All`) added two pipeline behaviors. They serialized the message instance with
`System.Text.Json` and Base64-encoded the JSON. The result became the `nservicebus.message.body` tag
on the current span.

Review raised:

- The messaging semantic conventions define `messaging.message.body.size` only. No convention attaches
  body content to a span.
- A body is a high-cardinality attribute. It can be large, and it can carry personal data or secrets.
  Base64 hides nothing.
- The backend indexes and stores the body at a cost per record.
- The behaviors serialized the message a second time. The pipeline already has the body as `byte[]` on
  the physical message.
- `JsonSerializer.Serialize(object, Type)` is marked `RequiresDynamicCode` and
  `RequiresUnreferencedCode`. It regresses the trimming and NativeAOT work.

## Decision

The option and the behaviors are removed. NServiceBus Core does not attach message bodies or message
properties to spans.

A user who needs payload data on a span writes a pipeline behavior. The behavior has the deserialized
message on `IIncomingLogicalMessageContext` or the raw body on `IIncomingPhysicalMessageContext`. It
checks `Activity.Current?.IsAllDataRequested` first and applies its own redaction.

## Consequences

- There is no built-in way to see a payload in a trace. The request will return. A future
  implementation has to meet these conditions:
  - work on the existing `byte[]`, without a second serialization;
  - write text bodies as UTF-8 and other bodies as Base64;
  - opt in per endpoint;
  - support redaction;
  - stay trimming-safe.
- The pipeline keeps no reflection over message types for telemetry. Header-to-tag promotion stays the
  only automatic span enrichment from message data.

## Alternative approaches

The point of leverage is a payload only on failure. It limits the volume, so it is the variant most
likely to be asked for again.

### Base64 of a JSON re-serialization

Implemented and removed. Double serialization, no sanitization, and a trimming regression.

### UTF-8 when the body is text, Base64 otherwise

Raised in review: probe for a NUL byte, or configure the encoding, or try UTF-8 and fall back. It fixes
the double serialization, not the data exposure or the backend cost. Not pursued.

### Payload only on failure

Raised in review and declined. The team had decided not to attach payloads at all.

### Public message properties as `nservicebus.message.{Property}` tags

The documentation of the option described this variant. It needs reflection per message and has the same
exposure. Not pursued.

### Log the payload instead of tagging it

Raised in review as a question. Not decided here.
