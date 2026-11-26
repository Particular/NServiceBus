# Span names include the queue or the message type name

**Date:** 2026-09-24
**Pull request:** [#7936](https://github.com/Particular/NServiceBus/pull/7936) (message type names in span names), [#7955](https://github.com/Particular/NServiceBus/pull/7955) (behind the v11 switch)
**Related:**

- [2026-10-02 single switch for the v11 behaviors](2026-10-02-single-switch-for-v11-opentelemetry-behaviors.md)
- [2026-10-21 no messaging semantic convention attributes](2026-10-21-no-messaging-semantic-convention-attributes.md)
- [OpenTelemetry messaging span names](https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/#span-name)
- [OpenTelemetry span name definition](https://opentelemetry.io/docs/specs/otel/trace/api/#span)
- [Upgrade guide 10 to 11, span names](https://github.com/Particular/docs.particular.net/blob/61c597df6c09b7bf57253fe697e5ff9f68d2a604/nservicebus/upgrades/10to11/index.md#span-names)

## Context

### Terms

- **Operation name**: `Activity.OperationName`. NServiceBus sets one constant per span type, for example
  `NServiceBus.Diagnostics.SendMessage`. It does not change in this decision.
- **Span name**: `Activity.DisplayName`. The OpenTelemetry .NET exporters send it as the span name. Backends
  group, search and alert on it.
- **Target**: the second part of a messaging span name, after the operation.

### Problem

In v10 the span names are generic: `process message`, `send message`, `publish event`. In a backend, all
processing of all endpoints is one group, and all sends are one group. A user must open each span or filter on
attributes to find one message flow.

The messaging semantic conventions use the format `{operation} {target}`. The target is usually the
destination. They require a target with low cardinality.

The OpenTelemetry span name definition and the Particular OpenTelemetry guidelines say:

- the name identifies a class of spans, not one span;
- the name contains only data that is available at compile time or at configuration time.

Constraints:

- **On the outgoing side, the destination is not known when the span starts.** The span starts in
  `MessageOperations`. The routing stage resolves the destination later, in the pipeline. A publish has no
  single destination.
- **On the incoming side, the message type is not a closed set.** A message can enclose several types. A
  sender can send a type that the receiver does not have. The header value comes from another process.
- **A changed span name breaks dashboards, alerts and searches without an error.** It cannot change by
  default in a minor release.

## Decision

1. **Span names use the format `{operation} {target}`** under the v11 behavior. In v11 this is the only format.
2. **Incoming, retry and recoverability spans use the queue as the target**: `process {receiveAddress}`,
   `immediate retry {receiveAddress}`, `delayed retry {receiveAddress}`, `move to {errorQueue}`. The queue is
   configuration-time data.
3. **Outgoing spans use the message type name as the target**: `publish {EventType}`,
   `subscribe {EventType}`, `unsubscribe {EventType}`. A subscribe for several event types lists the names
   separated by spaces. The operation is one verb, without the `message` or `event` of the v10 names.
4. **Send and reply spans also name the destination**: `send {MessageType} to {destination}` and
   `reply {MessageType} to {destination}`. The span starts as `send {MessageType}` or `reply {MessageType}`.
   After the routing stage resolves the destination, NServiceBus appends `to {destination}`. It does this only
   when the routing stage produces exactly one unicast transport operation. NServiceBus always routes a send or
   a reply to one destination. A user behavior can change `IRoutingContext.RoutingStrategies` to route it to
   several. The span then keeps the name without the destination, because one destination in the name would
   hide the others. The word `to` separates the two parts of the target, so a reader can tell the type from
   the queue.
5. **The type part of the target is `Type.Name`, not the full type name.** It contains no namespace, no
   assembly name, no version and no culture.
6. **The message type name counts as compile-time data, the destination as configuration-time data.** The
   types that an endpoint sends are compiled into the endpoint. The destinations come from the routing
   configuration and from the reply addresses of the senders, which are queues of the system. Neither changes
   while the endpoint runs. The code and the configuration bound the set, not the traffic. Thus the name obeys
   the span name rules above.
7. **The operation name does not change.** Code that filters on `Activity.OperationName` keeps working.
8. **The full type names stay in attributes.** `nservicebus.enclosed_message_types` and
   `nservicebus.event_types` carry them, as arrays under the v11 behavior.

## Consequences

- A user finds one message flow by span name in each backend, without a filter on attributes.
- The number of span names equals the number of operations times the number of queues and message types of the
  system. The code bounds this number, the traffic does not. Accepted.
- **Two types with the same short name in different namespaces get the same span name.** Accepted: the span
  name groups, it does not identify. Mitigation: the full type names are in the attributes of decision 8.
- **Samplers and `ActivityListener.ActivityStarted` callbacks see the send and reply span name without the
  destination.** The rename happens after the span starts. Accepted: the exporters read the name when the span
  ends, so backends get the full name. A sampler that must decide on the destination cannot use the span name.
- **A reply to a sender that uses a unique reply address per instance adds a span name per instance.** The
  number of instances is bounded by the deployment, not by the traffic. Accepted.
- Send and reply spans with the same message type and destination differ only in the operation, so a backend
  groups them apart.
- **A generic message type shows the metadata name**, for example ``Envelope`1``, not the type argument.
  Accepted: generic message types are rare.
- **A rename of a message type changes the span name.** A rename already breaks the wire contract, the
  routing and the subscriptions, so it is a planned change.
- Alerts, dashboards and searches on the v10 names must change. The upgrade guide lists the names. Users verify
  them on v10 with the `NServiceBus.Core.OpenTelemetry.UseV11Behavior` switch.
- The span names differ from the `{operation} {destination}` names of the transport SDK spans in the same
  trace. Accepted: the SDK span names the queue of the physical dispatch, the NServiceBus span names the logical
  operation. For send and reply, the NServiceBus span also names the destination.
- **Cost:** the outgoing span name is a new string for each operation, also when no span exists. The process
  span names are cached per receive address. A send or reply with a span builds one more string when the
  routing stage appends the destination. This is a trade-off between cost and simplicity, and simplicity
  was chosen:
  - Commit 4b1564f2d built the outgoing name only when a span exists. #8005 builds the name once, before the
    span starts, because the failure log also uses it.
  - [#8063](https://github.com/Particular/NServiceBus/pull/8063) built the name only when a span exists or a
    failure is logged. It added a struct for the parts of the name and changed `IActivityFactory`. It was
    closed.
  - The saving is about 96 B for each send, publish, reply, subscribe and unsubscribe without a listener. This
    is small compared to the headers, the contexts and the serialization of the same operation.
  - This diverges from the rule of the OpenTelemetry .NET instrumentation skill to build span names only after
    the check for a span.

### Keep the generic v10 names

Stable and with the lowest cardinality. Rejected: one group for all work of a kind does not help users find a
message flow. Users filter on attributes for each query.

### Use only the destination as the target of outgoing spans

This is the convention. Rejected: a publish has no single destination, and the destination is not known when
the span starts. Without the type, all sends to one queue are one group. Decision 4 adds the destination after
the type for send and reply, and accepts that samplers do not see it.

### Use only the message type as the target of send and reply spans

The state before decision 4 (`send message {MessageType}`). It names the span once, when it starts. Rejected: the destination is an important information the users should see immediatelly when looking at a span.

### Append the destination without a separator

The state before decision 4 also appended the destination after routing, as `send message {MessageType}
{destination}`. Rejected: both parts are free names, so a reader cannot tell where the type ends and the queue
starts.

### Keep the noun of the v10 operation

The v10 names are `send message`, `subscribe event` and `unsubscribe event`, but `publish event` became
`publish`. Rejected: the target already names the type, so the noun adds no information, and the mix made the
operations differ without a reason.

### Use the full type name

It identifies the type without collision. Rejected: the names get long, and the assembly-qualified name adds
the version, which changes on each release. The full name is in the attributes.

### Use the message type on incoming spans too

Rejected: the receiver does not control the set of types in the headers, and a message can enclose several
types. The queue is bounded and known at configuration time.

### An option that selects the span name format

The state before #7955 (`UseMessageTypeNamesInSpanNames`). Rejected with the other per-behavior toggles in the
ADR of 2026-10-02.
