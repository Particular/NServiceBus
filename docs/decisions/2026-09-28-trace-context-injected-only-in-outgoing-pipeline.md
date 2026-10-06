# Trace context is injected into headers only in the outgoing message pipeline

**Date:** 2026-09-28
**Pull request:** [#7952](https://github.com/Particular/NServiceBus/pull/7952)
**Related:**

- [2026-07-15 the sender decides trace continuation](2026-07-15-sender-decides-trace-continuation.md)

## Context

`RoutingToDispatchConnector` wrote `Activity.Current` into the headers of every message that reached the
routing stage. The routing stage is shared. These paths enter the pipeline at `IRoutingContext` without
passing the outgoing message pipeline:

- move to error
- delayed retry
- audit (`RouteToAudit`)
- `IMessageProcessingContext.ForwardCurrentMessageTo`
- the ServiceControl retry acknowledgement (`RetryAcknowledgementBehavior`)
- custom `RecoverabilityAction` and `AuditAction` implementations

For each of them the current activity replaced the `traceparent` of the forwarded message and added its
baggage. The error queue copy of a failed message then pointed at the processing span instead of the
original sender. `ForwardCurrentMessageTo` reuses the header dictionary of the incoming message, so the
in-flight message was mutated too.

Immediate retry and discard return no routing context. The subscribe and unsubscribe terminators
propagate the context themselves and dispatch directly, so they were not affected.

## Decision

1. `ContextPropagation.PropagateContextToHeaders` runs in `OutgoingPhysicalToRoutingConnector`. Only
   messages that flow through the outgoing message pipeline (send, publish, reply) reach it.
2. A control message that forks straight into the routing stage propagates explicitly.
   `RetryAcknowledgementBehavior` does so now, like the subscribe and unsubscribe terminators. The
   acknowledgement stays correlated to the processing of the retried message.
3. A forwarded message keeps its headers as received. The pipeline does not rewrite a trace header on a
   forwarded copy.
4. `TraceContextPreservationTests` runs each forwarding action through the real
   `RoutingToDispatchConnector` under an unrelated ambient activity. The tests assert that the received
   `traceparent` is kept and that no baggage is added. The header dictionary of the incoming message is
   asserted too.

## Consequences

- Error, audit and delayed-retry copies carry the trace context of the original sender. A ServiceControl
  retry starts a new trace that links to the original sender span. Recoverability metadata sets the
  `StartNewTrace` header on a move to error.
- A forwarded message does not record the span of the attempt that forwarded it. Accepted: the processing
  span and the recoverability span are in the trace of the sender span, or linked to it.
- Custom `RecoverabilityAction` and `AuditAction` implementations get the same behavior without a change.
- The rule for new code: a path that enters at `IRoutingContext` gets no trace context by default. A new
  control message must propagate explicitly, like the three that do today. This is easy to miss, which
  is why this record exists.
- A unit test that pins the call to `OutgoingPhysicalToRoutingConnector` was not added. The acceptance
  tests assert `traceparent` and `baggage` on the wire. A class-named test would only pin the location.

## Alternative approaches

The point of leverage is stripping the trace headers from forwarded messages. It decides what
ServiceControl sees on a retry. The first and the third option below were not raised during the work.
They are listed because a reader is likely to propose them.

### Keep the injection in the routing stage and skip when `traceparent` is already present

The rule would then depend on header presence. A message built from a copy of the incoming headers would
keep stale context. A forwarded message without a trace header would still get the wrong one. Rejected.

### Strip the trace headers from forwarded messages before the routing stage

The error queue message would then lose the sender context that a ServiceControl retry needs. Rejected.

### Mark forwarding routing contexts and let the connector check the mark

Adds state to every forwarding path to protect one call. Moving the call to the stage that only the
outgoing pipeline uses needs no mark. Rejected.
