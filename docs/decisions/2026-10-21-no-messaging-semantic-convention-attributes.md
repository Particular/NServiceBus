# NServiceBus does not emit messaging semantic convention attributes

**Date:** 2026-10-21
**Pull request:** [#7962](https://github.com/Particular/NServiceBus/pull/7962)
**Related:**

- [#7936](https://github.com/Particular/NServiceBus/pull/7936) span names follow the `{operation} {target}` format
- [#7949](https://github.com/Particular/NServiceBus/pull/7949) the incoming span is a child of the ambient transport SDK span
- [OpenTelemetry messaging semantic conventions](https://opentelemetry.io/docs/specs/semconv/messaging/)

## Context

The OpenTelemetry messaging semantic conventions define attributes such as `messaging.system`,
`messaging.destination.name`, `messaging.operation.type` and `messaging.message.id`. The conventions are not
stable. Some .NET messaging frameworks, such as Brighter, emit them on their own spans.

Instrumented transport SDKs, such as the Azure Service Bus client, emit these attributes on their send and
receive spans. With the v11 behavior, the NServiceBus incoming span is a child of the SDK receive span (#7949),
so both spans are in the same trace.

NServiceBus concepts do not map one-to-one to the native ones:

- the logical message ID is not the native message ID;
- an endpoint is not a queue, and a message can be routed through several;
- the message intent is not a messaging operation type.

The same attribute name with two values in one trace makes queries match the wrong span. Backends that build
queue views or service maps from `messaging.destination.name` count the message twice.

## Decision

1. **All NServiceBus attributes use the `nservicebus.*` namespace**, on spans and on metrics.
2. **Messaging semantic convention attributes are left to the transport SDK instrumentation.**
3. **General semantic convention attributes with one meaning are allowed.** Today that is only `error.type`.
4. **Span names keep the `{operation} {target}` format.** A name is not an attribute and does not conflict.
5. The rule is stated in a comment in `ActivityTags` and `MeterTags`.

## Consequences

- Backend views keyed on `messaging.*` do not recognize NServiceBus spans. Mitigation: a processor or collector
  transform maps `nservicebus.*` to `messaging.*`. The docs describe this.
- Transports without an instrumented SDK, such as SQL Server, PostgreSQL, MSMQ and the learning transport,
  have no `messaging.*` attributes at all. Accepted, with the same mitigation.
- A rename in the unstable conventions does not break users. `nservicebus.*` names change only in a major.
- Follow-up: add the comment to `ActivityTags` and `MeterTags`, and document the rule on docs.particular.net.
- Follow-up: revisit when the messaging conventions are stable.

## Alternative approaches

The point of leverage is who owns `messaging.*`: NServiceBus or the transport SDK. It returns whenever a user asks
for out-of-the-box messaging views.

### Emit `messaging.*` on NServiceBus spans

The approach Brighter takes. Rejected: it conflicts with the SDK spans in the same trace, and the conventions
can still change.

### Emit `messaging.*` only when no SDK span exists

Rejected. NServiceBus cannot detect an SDK span on every path, for example on the send side. Attribute presence
would then depend on the transport, and the same query would behave differently per transport.

### An opt-in option to emit `messaging.*`

Credible, and likely to come back. Rejected for now: a collector transform does the same without new public API
that would need an obsoletion cycle.
