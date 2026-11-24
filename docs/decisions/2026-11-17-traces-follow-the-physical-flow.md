# Traces show the physical flow of messages; logical relations are links

**Date:** 2026-11-19
**Pull request:** https://github.com/Particular/NServiceBus/pull/8051
**Related:**

- [2026-09-23 NServiceBus trace parent header and ambient parent](2026-09-23-trace-parent-header-and-ambient-parent.md) applies this rule on receive
- [2026-07-15 the sender decides trace continuation](2026-07-15-sender-decides-trace-continuation.md) starts a new trace and links to the sender
- [2026-09-28 trace context is injected only in the outgoing pipeline](2026-09-28-trace-context-injected-only-in-outgoing-pipeline.md) keeps the sender context on forwarded copies
- [2026-10-01 receive-side trace state and baggage propagation](2026-10-01-receive-side-trace-state-and-baggage-propagation.md) uses the logical send context for baggage
- [2026-10-21 no messaging semantic convention attributes](2026-10-21-no-messaging-semantic-convention-attributes.md) leaves the physical attributes to the SDK spans

## Context

### Terms

- **Physical activity**: work that runs in one process at one time. Examples: an SDK receive, one processing
  attempt, a handler call, a dispatch to the broker.
- **Logical relation**: a relation in the intent of the code, not in the sequence of execution. Examples: the
  `Send` call that made a message, the original sender of a retried message, the failed attempt that a
  recoverability decision is about.
- **Parent**: the span that a span is a child of. A trace is a tree of parents.
- **Link**: a reference to a span in this trace or in a different trace. A link is not a parent.

### Problem

In a messaging system, physical activities and logical relations are different:

- **The send is not the dispatch.** With the outbox, batched dispatch, the messaging bridge and ServiceControl
  retries, `Send` and the dispatch occur at different times. They can occur in different processes.
- **Transport SDKs trace the physical work.** Azure Service Bus, RabbitMQ.Client 7 and the AWS contrib
  instrumentation make their own send and receive spans. On receive, the SDK span is `Activity.Current` when
  the NServiceBus pipeline starts.
- **One message can have many attempts.** Immediate retries, delayed retries and ServiceControl retries process
  the same message again.

If NServiceBus uses the outgoing send span as the parent, the trace loses important physical information about
what occurred. The receive span becomes a child of work that stopped before it, or of work in a different
process. The SDK spans are then siblings of the NServiceBus spans, not parents.

Constraints:

- **The v10 trace shape stays the default in minor releases.** Until v11, the incoming span is a child of the
  sender span (ADR of 2026-10-02).
- **Not all backends support links.** Such a backend shows the trace as separate parts at each link.
- **NServiceBus cannot identify the instrumented SDK.** It sees only `Activity.Current`.
- **Sampling follows the parent.** A child gets the sampling decision of its parent. A link does not.

## Decision

1. **NServiceBus shows the physical work of the platform as it is.** A span is an activity that ran, at the time
   and in the process where it ran. NServiceBus does not change, merge or hide spans to show a logical view.
2. **Parents follow the physical flow.** The parent of a span is the activity that ran when the work started.
   On receive, this is the ambient SDK span, if there is one.
3. **Logical relations are links.** These are links, not parents: the `Send` span, the original sender of a
   retried message, and the failed attempt of a recoverability decision.
4. **This rule is only for parents and links.** For tags, see the ADR of 2026-10-21. For trace state and
   baggage, see the ADR of 2026-10-01.

## Consequences

- A trace shows what ran, in the sequence that it ran. On receive, the SDK spans and the NServiceBus spans are
  in one tree. A query for a broker operation and the processing that it caused does not need to join traces.
- The SDK controls the size of the trace. If the SDK receive span continues the trace of the incoming message,
  a full [message conversation](https://docs.particular.net/nservicebus/messaging/headers#messaging-interaction-headers-nservicebus-conversationid)
  becomes a single trace (one span tree).
- The SDK also controls the sampling decision. If the SDK receive span starts a new trace, the NServiceBus span
  uses the sampling decision of that new trace, not the decision of the sender. Example: both endpoints use
  `ParentBased(TraceIdRatioBased(0.1))`. The sender is sampled. The SDK receive span is a new root, so the
  ratio sampler decides again, and 90% of these receives are not sampled. The NServiceBus processing span is
  then not recorded, but the sender span is recorded. If the SDK receive span continues the incoming trace, the
  sampling decision of the sender applies, as in v10.
- The SDK sets the relation between its send span and its receive span. NServiceBus does not change it, and
  does not enforce the OpenTelemetry recommendation of a link on the SDKs. On 2026-11-24 the relations are:

  | SDK | SDK receive span relation to the send span | NServiceBus incoming span |
  |---|---|---|
  | RabbitMQ.Client 7 | Child of the publish span, plus a link. `UsePublisherAsParent = false` gives a link only. | Child of the SDK deliver span |
  | Azure.Messaging.ServiceBus (with `Azure.Experimental.EnableActivitySource`) | The process span is a child of the message context, plus a link | Child of the SDK process span |
  | Amazon SQS (OpenTelemetry.Instrumentation.AWS) | No SDK receive span reads the context | Child of the sender span, as in v10 |

  With the current SDKs, a message flow stays in one trace on all three transports. If an SDK changes to
  links, the trace shape on that transport changes without a change in NServiceBus. Accepted; revisit when an
  SDK changes its relation or the messaging conventions are stable.
- The parent of the incoming span depends on the tracer configuration, not only on the transport. On the same
  transport, the parent is the SDK span when the application subscribes to the SDK ActivitySource, and the
  sender span when it does not. Accepted; the upgrade guide documents it (docs.particular.net#8443).
- Currently, the SDKs do not propagate baggage. NServiceBus propagates baggage from the logical send context.
  If the SDKs start to propagate baggage, this behavior can change.

## Alternative approaches

The logical trace is the point of leverage: one tree for each message flow, with the SDK spans at the side.
Users of backends without link support ask for it. It is also the v10 shape.

### Logical parents, physical links

The sender span is always the parent. The SDK span is a link or a sibling. This is the v10 default. It gives
one trace for each message flow in all backends. Rejected: the incoming span is then a child of a span that
stopped before it started, often in a different process. The SDK span that delivered the message is a sibling.
With the outbox and the bridge, the parent is not the source of the message.

### A configurable shape, per endpoint or per message

A setting selects physical or logical parents. This is credible because both shapes have users. Rejected: a
setting adds too many permutations. It also does not agree with this decision, where the SDK decides the shape
of the trace. The ADR of 2026-09-23 also rejects a sender-side setting, `TraceMode.UseExisting`, because only
the receiver knows if an SDK span exists.
