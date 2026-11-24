# Span parenting is left to the transport SDK

**Date:** 2026-11-19
**Pull request:** TBD
**Related:**

- [#7949](https://github.com/Particular/NServiceBus/pull/7949) the incoming span is a child of the ambient transport SDK span
- [Traces show the physical flow of messages; logical relations are links](2026-11-17-traces-follow-the-physical-flow.md)
- [NServiceBus does not emit messaging semantic convention attributes](2026-10-21-no-messaging-semantic-convention-attributes.md)
- [OpenTelemetry messaging semantic conventions](https://opentelemetry.io/docs/specs/semconv/messaging/)

## Context

The OpenTelemetry messaging semantic conventions define the trace structure. They recommend a link between the
span that sends a message and the span that receives it.

The instrumented SDKs that we examined do not follow this recommendation. The RabbitMQ and Azure Service Bus
SDKs make the receive span a child of the send span, and also add a link. Thus, the send and the receive
operations are in one trace. RabbitMQ.Client 7 has an option, `UsePublisherAsParent`, that changes this to a
link only. The Amazon SQS instrumentation injects the trace context on send, but creates no receive span that
carries it. Thus, Amazon SQS is a transport without an instrumented receive, see point 3.

NServiceBus is a framework that runs on top of these SDKs. The incoming span is a child of the SDK receive
span (#7949). Thus, the SDK sets the trace structure, and NServiceBus uses it.

Users can configure how NServiceBus manages the relation between the send span and the receive span. This
decision is about the default behavior. The default applies when the user does not configure the relation.

## Decision

1. **By default, span parenting rules are left to the transport SDK instrumentation.** NServiceBus does not
   change the parent of an SDK span, and does not replace a parent-child relation with a link. If the SDK makes
   the receive span a child of the send span, NServiceBus keeps that relation.
2. **It is not the responsibility of NServiceBus to enforce the OpenTelemetry recommendation on the SDKs.**
3. **On transports without an instrumented SDK, NServiceBus sets the relation.** By default, the incoming span
   is a child of the send span. This is the same relation that the SDKs use, so the trace structure is the same
   on all transports.
4. **A user who wants links configures this explicitly.** The configured behavior replaces the default.

## Consequences

- By default, the trace structure does not agree with the OpenTelemetry recommendation, on all transports.
  A long message flow, such as a saga, can make one very large trace. Accepted: the user can configure links.
- If an SDK changes to links, the default NServiceBus trace structure on that transport changes with it,
  without a change in NServiceBus. Users can see different traces after an SDK update. The trace structure is
  then different on transports with and without an instrumented SDK.
- NServiceBus cannot identify an SDK span. It uses `Activity.Current` when the receive pipeline starts. If a
  different span is current, for example a span from custom transport code or a hosting span that was not
  stopped, the incoming span becomes its child and the send span is only a link. The incoming span is then not
  in the trace of the sender. Accepted and documented. A possible mitigation is to accept only a current span of
  kind `Consumer`; not implemented.
- The parent of the incoming span depends on the tracer configuration, not only on the transport. On the same
  transport, the parent is the SDK span when the user subscribes to the SDK ActivitySource, and the send span
  when the user does not. Point 3 makes the trace depth the same, but not the parent span.
- The SDK controls sampling of the incoming span. With a child relation, the decision of the sender applies,
  as in v10. If the SDK starts a new trace, the sampler decides again, and the incoming span can be dropped
  while the send span is recorded.
- Follow-up: document on docs.particular.net that the SDK sets the default span parenting, which relation each
  SDK uses today, and how to configure links.
- Follow-up: revisit when the messaging conventions are stable, or when an SDK changes its relation.

## Alternative approaches

### Use links by default on transports without an instrumented SDK

This agrees with the OpenTelemetry recommendation. Rejected: the default trace structure would then depend on
the transport. A user who changes from SQL Server to RabbitMQ would see different traces without a change in
the configuration.
