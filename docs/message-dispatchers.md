# Message dispatchers

Transports implement `IMessageDispatcher` to send messages for core. A transport has exactly one dispatcher instance for the lifetime of the endpoint; core registers `TransportInfrastructure.Dispatcher` in the container as a singleton and shares it across the send/publish/reply pipeline, recoverability, satellite recoverability, and message-driven subscribe/unsubscribe.

Core issues no corresponding `Initialize`, start, or stop call for the dispatcher: it must be ready to dispatch as soon as `TransportDefinition.Initialize` returns, and must keep working until `TransportInfrastructure.Shutdown` is called, including while receivers are stopping, because `StopReceive` waits for in-flight `OnMessage` and `OnError` invocations that may still dispatch.

A transport must support concurrent calls to `Dispatch`: several messages can be mid-processing at once, each able to reach the dispatcher independently, and a send made outside message processing can happen at the same time.

## Call sequence

Core calls `Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken)` from several places:

- The send, publish, and reply pipelines, once their routing and serialization stages have produced one or more `TransportOperation`s.
- The main receiver's recoverability pipeline, which routes the recoverability action's own outgoing messages (the error queue copy, a delayed retry, or any other message a custom recoverability action produces) through the same routing and dispatch stages as a regular send. `SatelliteRecoverabilityExecutor` does the same for satellites, calling `IMessageDispatcher.Dispatch` directly instead of going through the pipeline. See [Dispatching from recoverability](recoverability.md#dispatching-from-recoverability).
- `MessageDrivenSubscribeTerminator`, `MessageDrivenUnsubscribeTerminator`, `MigrationSubscribeTerminator`, and `MigrationUnsubscribeTerminator`, to send the `Subscribe` and `Unsubscribe` control messages that message-driven publish/subscribe and subscription migration mode use. See [Subscriptions](infrastructure-setup.md#subscriptions).

A single logical operation, such as one `Send` or `Publish` call, can resolve to more than one `TransportOperation` when it has more than one routing strategy, for example a `Publish` with several subscribers. When that happens, core gives each resulting operation its own copy of the message headers and of the dispatch properties, so a transport that mutates one operation's headers or properties does not affect another operation produced by the same logical call. With a single routing strategy, the headers and properties are passed through by reference instead of copied.

### Batching

Core does not call `Dispatch` once per outgoing message. While a handler processes an incoming message, operations raised with `DispatchConsistency.Default` are collected in a `PendingTransportOperations` instance attached to the message's context instead of being dispatched immediately. Once the handler pipeline for that message completes (after the outbox transaction commits, when the outbox is enabled), core calls `Dispatch` once with every operation collected for that message, as a single `TransportOperations` batch, before the receive is completed. A transport can treat the operations passed to one call as eligible to be sent together.

Operations bypass batching and reach `Dispatch` immediately, on their own, when:

- The caller used `RequireImmediateDispatch()`, which sets `DispatchConsistency.Isolated` on the operation.
- There is no `PendingTransportOperations` in the current context, which is the case for any send, publish, or reply made outside of handling an incoming message, for example from a timer or a hosted service.

Dispatches made from recoverability and from message-driven subscribe/unsubscribe are never collected into a batch either, because they do not run inside the `PendingTransportOperations` scope that a message's own handler pipeline uses.

## Transport operations

`TransportOperations` splits what core asks a transport to send into two lists:

- `UnicastTransportOperations`, a `List<UnicastTransportOperation>`, each with a `Destination` naming a single receiving queue.
- `MulticastTransportOperations`, a `List<MulticastTransportOperation>`, each with a `MessageType` naming the event to deliver to every current subscriber.

Both operation types implement `IOutgoingTransportOperation` and expose:

- `Message`, the `OutgoingMessage` to send: a `MessageId`, the `Headers` dictionary, and the `Body`.
- `Properties`, a `DispatchProperties` instance; see [Delivery properties](#delivery-properties).
- `RequiredDispatchConsistency`; see [Dispatch consistency](#dispatch-consistency).

A `MulticastTransportOperation` requires `TransportDefinition.SupportsPublishSubscribe` to be `true`. When it is `false`, core instead routes publishes through message-driven publish/subscribe, which turns a publish into one `UnicastTransportOperation` per known subscriber.

## Dispatch consistency

`DispatchConsistency` tells the transport whether the operation may be tied to the current receive:

- `Default`: the transport may enlist the operation in the ongoing receive transaction when `TransportTransactionMode` and the current `TransportTransaction` allow it. Outside of message processing, or when nothing is being received, there is no receive transaction to enlist in, and the operation is simply sent.
- `Isolated`: the transport must send the operation outside any ongoing receive transaction, in every transaction mode, so it reaches its destination even when the attempt that produced it later fails.

A transport must read `RequiredDispatchConsistency` per operation, not once for the whole call: nothing in `TransportOperations` guarantees that every operation passed to one `Dispatch` call shares the same value. In practice, every batch core assembles today is uniform, but the dispatcher is also the entry point for raw, non-hosted use of a transport, where a caller is free to mix `Default` and `Isolated` operations in one `TransportOperations`.

A batch of operations collected through `PendingTransportOperations` always carries `DispatchConsistency.Default`, because operations created with `RequireImmediateDispatch()` never enter that batch. When the outbox is enabled, a pipeline behavior rewrites every operation in the batch to `Isolated` before it reaches the dispatcher, immediately after the outbox transaction commits: the message is already durably recorded, so the send must not depend on, or roll back with, the receive that is about to be acknowledged. [Dispatching from recoverability](recoverability.md#dispatching-from-recoverability) describes the per-`TransportTransactionMode` atomicity rules a transport must follow for `Default` and `Isolated` dispatches; those rules are not specific to recoverability, they describe what each transaction mode means for every dispatch made while a message is being processed.

The `TransportTransaction` instance passed to `Dispatch` is the same instance used for the current receive, when one exists, or an empty one created on demand otherwise. A transport that reads a native transaction object out of it to enlist an operation must do so only when `RequiredDispatchConsistency` is `Default`.

## Delivery properties

`DispatchProperties` carries per-operation delivery constraints as string key/value pairs, with three well-known ones exposed as typed properties:

- `DoNotDeliverBefore`: deliver the message no earlier than a specific `DateTimeOffset`. Requires `TransportDefinition.SupportsDelayedDelivery`.
- `DelayDeliveryWith`: deliver the message no earlier than after a `TimeSpan`. Also requires `TransportDefinition.SupportsDelayedDelivery`.
- `DiscardIfNotReceivedBefore`: discard the message if it has not been received within a `TimeSpan`. Requires `TransportDefinition.SupportsTTBR`.

Core checks these capability flags before an operation reaches the dispatcher: it throws at startup when a message is attributed with `[TimeToBeReceived]` but the transport's `SupportsTTBR` is `false`, and it throws when a handler tries to defer a message but the transport's `SupportsDelayedDelivery` is `false`. Core does not prevent an operation from carrying both a delay (`DoNotDeliverBefore` or `DelayDeliveryWith`) and `DiscardIfNotReceivedBefore` at the same time, even though the transport supports both independently; a transport must decide what that combination means, including rejecting it outright.

A transport may also read and write its own keys on the same `DispatchProperties` bag to round-trip transport-specific data between receiving and a later dispatch of the same logical message, for example to recreate a native property on a delayed retry. Properties set in `ReceiveProperties` on the incoming message are copied into the outgoing operation's `DispatchProperties` whenever the outgoing message's ID matches the incoming message's ID and the property is not already set, which covers audit, error, and retry dispatches; see [`ReceiveProperties`](recoverability.md#reporting-a-failure).

## Outbox interaction

When the outbox is enabled, `TransportReceiveToPhysicalMessageConnector` does not fork into the batched dispatch stage until the outbox transaction that recorded the outgoing operations has committed. The operations are also persisted as part of the outbox record, serialized with their routing folded into `DispatchProperties` (`Destination` for a unicast operation, `EventType` for a multicast one).

When the outbox detects that a message has already been processed, it rebuilds the same operations from that record and dispatches them again with `DispatchConsistency.Isolated`, so a transport must be able to reconstruct and resend an operation purely from the `OutgoingMessage` and `DispatchProperties` it is given, without relying on anything from the original dispatch call. The outbox requires `TransportTransactionMode.ReceiveOnly`; core refuses to start otherwise.
