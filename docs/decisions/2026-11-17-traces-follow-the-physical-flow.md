# Traces show the physical flow of messages; logical relations are links

**Date:** 2026-11-17 <!-- TODO: the date the decision was made -->
**Pull request:** TODO
**Related:**

- [2026-09-23 NServiceBus trace parent header and ambient parent](2026-09-23-trace-parent-header-and-ambient-parent.md) applies this rule on receive
- [2026-07-15 the sender decides trace continuation](2026-07-15-sender-decides-trace-continuation.md) decides where a trace starts and links back
- [2026-09-28 trace context is injected only in the outgoing pipeline](2026-09-28-trace-context-injected-only-in-outgoing-pipeline.md) keeps the sender context on forwarded copies
- [2026-10-01 receive-side trace state and baggage propagation](2026-10-01-receive-side-trace-state-and-baggage-propagation.md) prefers the logical send context for baggage
- [2026-10-21 no messaging semantic convention attributes](2026-10-21-no-messaging-semantic-convention-attributes.md) leaves the physical attributes to the SDK spans

## Context

### Terms

- Physical activity: work that runs in a process at a point in time. Examples: a transport SDK receiving a
  message, NServiceBus processing one attempt of a message, a handler invocation, a dispatch to the broker.
- Logical relation: a relation that exists in the intent of the code, not in the order of execution.
  Examples: the `Send` call that produced a message, the original sender of a message that is retried from
  the error queue, the failed attempt that a recoverability decision is about.
- Parent: the span a span is a child of. A trace is the tree of parents.
- Link: a reference from one span to another, in the same trace or another one, without a parent relation.

### The problem

Physical activities and logical relations do not line up in a messaging system:

- **The send is not the dispatch.** Under the outbox, batched dispatch, the messaging bridge and
  ServiceControl retries, the `Send` call and the dispatch to the broker happen at different times, and
  can be in different processes.
- **Transport SDKs trace the physical work.** Azure Service Bus, RabbitMQ.Client 7 and the AWS contrib
  instrumentation create their own send and receive spans. On receive, the SDK span is `Activity.Current`
  when the NServiceBus pipeline starts.
- **One message, many attempts.** Immediate retries, delayed retries and a ServiceControl retry each process
  the same logical message again.

A trace can follow either structure. Following the logical one makes a span the child of work that already
ended, or that ran in another process, and puts SDK spans beside the NServiceBus spans instead of above them.
Each past decision picked a structure for one path. Without a rule, the next path is decided again from
scratch.

Constraints:

- **The v10 trace shape stays the default in a minor release.** The incoming span is a child of the sender
  span until v11 (see the ADR of 2026-10-02).
- **Backends differ in link support.** A backend without link support shows a trace split at every link.
- **NServiceBus cannot detect which SDK is instrumented.** It only sees `Activity.Current`.
- **Sampling follows the parent.** A child inherits the sampling decision of its parent. A link does not.

<!-- TODO: add the reason the platform wants this now, e.g. the problem users reported or the research that
     triggered it. Keep internal sources out; describe the evidence instead. -->

## Decision

1. **NServiceBus exposes the physical workings of the platform as they are.** Spans represent activities that
   ran, at the time and in the process they ran in. NServiceBus does not reshape, merge or hide spans to
   present a logical view.
2. **Parent relations follow the physical flow.** The parent of a span is the activity that was running when
   the work started. On receive, that is the ambient transport SDK span when there is one.
3. **Logical relations are links.** The span of the `Send` call, the original sender of a retried message and
   the failed attempt a recoverability decision is about are linked, not parents.
4. **This rule covers span parents and links only.** What a span is tagged with is covered by the ADR of
   2026-10-21. Trace state and baggage are covered by the ADR of 2026-10-01.

How the existing decisions map to the rule:

| Path | Parent | Link | Record |
|---|---|---|---|
| Receive under an SDK span (v11) | SDK receive span | sender span | 2026-09-23 decision 3 |
| Receive without an SDK span | sender span | none | 2026-09-23, see open point 1 |
| Delayed send, saga timeout, delayed retry, publish in v10 | none, new trace | sender span | 2026-07-15 |
| Recoverability decision | as for receive: the ambient span, else the sender span | innermost failed span | 2026-10-26 decisions 7 and 10 |
| Move to error, audit, forward | not recorded | original sender context kept on the copy | 2026-09-28, see open point 2 |
| ServiceControl retry | none, new trace | original sender span | 2026-09-28 |

## Consequences

- A trace shows what ran, in the order it ran. The SDK spans and the NServiceBus spans form one tree on
  receive. Queries for a broker operation and the processing it caused need no join across traces.
- A backend without link support shows the trace split at every logical relation. Accepted, as in the ADR
  of 2026-09-23: the physical parent is the real parent of the work. A user who wants one trace per message
  flow can turn the SDK instrumentation off. <!-- TODO: quantify, e.g. which supported backends lack links -->
- Under the v11 behavior the sampling decision of the SDK span applies to the NServiceBus span, not the
  decision of the sender. <!-- TODO: accept or mitigate -->
- New paths are decided by the rule: a new span gets the running activity as parent, and its logical origin
  as a link.

Open points. These are places where a recorded decision does not follow the rule yet. This record does not
change them:

1. **No dispatch span.** Without an SDK span the receiver's parent is the NServiceBus send span. Under the
   outbox, the send span ended before the dispatch ran, so the parent is a logical relation. Following the
   rule needs a span for the physical dispatch. Not decided.
2. **Forwarded copies keep the original sender context.** The ADR of 2026-09-28 keeps the received
   `traceparent` on the error, audit and delayed-retry copies, so a ServiceControl retry links to the original
   sender. Physically, the copy was dispatched by the processing attempt that failed. That ADR accepts that
   the forwarding attempt is not recorded. Whether the copy should carry the forwarding attempt as parent and
   the original sender as link is not decided.
3. **Baggage follows the logical send context.** The ADR of 2026-10-01 rejects relying on the SDKs, partly
   because they propagate the context at physical dispatch, not the logical one. That is consistent with
   decision 4, because baggage is data, not structure. It must stay consistent if a dispatch span is added
   (open point 1).
4. **The v10 default is the logical shape.** Until v11, the incoming span is a child of the sender span also
   when an SDK span is ambient. The rule fully applies only with the v11 behavior.

## Alternative approaches

The point of leverage is the logical trace: one tree per message flow, with the SDK spans to the side. It is
what users of backends without link support ask for, and it is the v10 shape.

### Logical parents, physical links

The sender span is always the parent and the SDK span a link or a sibling. This is the v10 default. It gives
one trace per message flow in every backend. Rejected: the incoming span then becomes the child of a span that
ended before it started, often in another process, and the SDK span that actually delivered the message sits
beside it. Under the outbox and the bridge the parent is not where the message came from.
<!-- TODO: confirm, and add any reason from the review discussion -->

### A configurable shape, per endpoint or per message

A setting that chooses physical or logical parents. Credible, because both shapes have users. A sender-side
version was tried as `TraceMode.UseExisting` and rejected in the ADR of 2026-09-23: whether an SDK span
exists is a fact of the receiver. A receiver-side setting conflicts with the ADR of 2026-07-15, where the
sender decides the trace shape. <!-- TODO: confirm this is why a receiver-side setting is rejected -->

### Synthetic logical spans

NServiceBus adds a span for the logical message flow, for example from `Send` until the last processing
attempt ends, and parents physical spans on it. Rejected: the span does not represent work that ran, it can
stay open across processes and retries, and no process owns ending it. The ADR of 2026-10-26 rejects storing an
`Activity` in the context bag for the same reason.
