# Message dispatchers

Transports implement `IMessageDispatcher` to send messages for core. Core registers `TransportInfrastructure.Dispatcher` as a singleton, so one dispatcher instance serves sends, publishes, replies, recoverability, and subscriptions for the lifetime of the endpoint.

Core does not initialize, start, or stop the dispatcher. It must be ready to dispatch when `TransportDefinition.Initialize` returns and keep working until `TransportInfrastructure.Shutdown` is called. This includes the time while receivers are stopping, because `StopReceive` waits for in-flight `OnMessage` and `OnError` invocations that may still dispatch.

A transport must support concurrent calls to `Dispatch`, because several messages can be processed at once and sends can also happen outside message processing.

## Call sequence

Core calls `Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken)` from several places:

- The send, publish, and reply pipelines, once their routing and serialization stages have produced one or more `TransportOperation`s.
- Recoverability, for the error queue copy, a delayed retry, or any other message a custom recoverability action produces. See [Dispatching from recoverability](recoverability.md#dispatching-from-recoverability).
- Subscribing and unsubscribing, to send the `Subscribe` and `Unsubscribe` control messages that message-driven publish/subscribe and subscription migration mode use. See [Subscriptions](infrastructure-setup.md#subscriptions).

One `Send` or `Publish` call can resolve to several `TransportOperation`s, for example a `Publish` with several subscribers. Each of those operations gets its own copy of the headers and dispatch properties, so a transport can mutate one without affecting the others. When the call resolves to a single operation, core does not copy them.

### Batching

While a handler processes an incoming message, core collects the operations raised with `DispatchConsistency.Default` in a batch for that message. When the handler pipeline completes, core passes the whole batch to one `Dispatch` call before the receive is completed. [Outbox interaction](#outbox-interaction) describes how the outbox changes this batch. A transport can treat the operations passed to one call as eligible to be sent together.

Core does not preserve the order in which a handler raised the operations in a batch; the batch currently arrives newest first. A transport that must send them in order has to record the order itself, for example in a `DispatchProperties` key set by a pipeline behavior.

Operations reach `Dispatch` immediately, outside any batch, when:

- The caller used `RequireImmediateDispatch()`, which sets `DispatchConsistency.Isolated` on the operation.
- The send, publish, or reply is made outside of handling an incoming message, for example from a timer or a hosted service.

Dispatches from recoverability and from subscribing and unsubscribing are never batched either, because they do not run inside a message's handler pipeline.

## Transport operations

`TransportOperations` contains two lists:

- `UnicastTransportOperations`, each with a `Destination` naming a single receiving queue.
- `MulticastTransportOperations`, each with a `MessageType` naming the event to deliver to every current subscriber.

Both operation types implement `IOutgoingTransportOperation` and expose:

- `Message`, the `OutgoingMessage` to send: a `MessageId`, the `Headers` dictionary, and the `Body`.
- `Properties`, a `DispatchProperties` instance; see [Delivery properties](#delivery-properties).
- `RequiredDispatchConsistency`; see [Dispatch consistency](#dispatch-consistency).

Core produces a `MulticastTransportOperation` only when `TransportDefinition.SupportsPublishSubscribe` is `true`. Otherwise, message-driven publish/subscribe turns a publish into one `UnicastTransportOperation` per known subscriber.

## Dispatch consistency

`DispatchConsistency` tells the transport whether the operation may be tied to the current receive:

- `Default`: the transport may enlist the operation in the ongoing receive transaction when `TransportTransactionMode` and the current `TransportTransaction` allow it.
- `Isolated`: the transport must send the operation outside any ongoing receive transaction, in every transaction mode, so it reaches its destination even when the attempt that produced it later fails.

A transport must read `RequiredDispatchConsistency` per operation, because one `Dispatch` call can mix `Default` and `Isolated` operations. Batches that core assembles are uniform today, but a caller that uses the transport without an endpoint can mix them.

For `Default` operations dispatched while a message is being processed, `TransportTransactionMode` sets the atomicity rules:

- In `TransportTransactionMode.SendsAtomicWithReceive` and `TransportTransactionMode.TransactionScope`, the operations must commit atomically with completing the receive. When the attempt fails, they are rolled back with it and must never reach their destination.
- In `TransportTransactionMode.ReceiveOnly` and `TransportTransactionMode.None`, sends are not atomic with the receive. An operation can reach its destination even when completing the receive later fails.

[Dispatching from recoverability](recoverability.md#dispatching-from-recoverability) describes how these rules apply to dispatches made from `OnError`.

The `TransportTransaction` passed to `Dispatch` is usually the current receive's instance, or an empty one when there is no receive. A caller can also set its own `TransportTransaction` in the extensions of `SendOptions` or `PublishOptions`. Core passes that instance to `Dispatch` when the operation is not batched. A transport must not enlist an `Isolated` operation in the receive transaction, but it may enlist one in a transaction the caller supplied this way.

## Delivery properties

`DispatchProperties` holds per-operation delivery constraints as string key/value pairs. Three well-known ones have typed properties:

- `DoNotDeliverBefore`: deliver the message no earlier than a specific `DateTimeOffset`. Requires `TransportDefinition.SupportsDelayedDelivery`.
- `DelayDeliveryWith`: deliver the message no earlier than after a `TimeSpan`. Also requires `TransportDefinition.SupportsDelayedDelivery`.
- `DiscardIfNotReceivedBefore`: discard the message if it has not been received within a `TimeSpan`. Requires `TransportDefinition.SupportsTTBR`.

Core enforces these capability flags before an operation reaches the dispatcher. It does not prevent one operation from carrying both a delay (`DoNotDeliverBefore` or `DelayDeliveryWith`) and `DiscardIfNotReceivedBefore`. The transport decides what that combination means and may reject it.

A transport may add its own keys to carry transport-specific data from a receive to a later dispatch of the same message, for example to recreate a native property on a delayed retry. When the outgoing message ID matches the incoming one, core copies each [`ReceiveProperties`](recoverability.md#reporting-a-failure) entry into `DispatchProperties` unless the key is already set. This covers audit, error, and retry dispatches.

## Outbox interaction

When the outbox is enabled, core dispatches the batch only after the outbox transaction that recorded its operations has committed. Core then changes every operation in the batch to `DispatchConsistency.Isolated`, because the message is already durably recorded and the send must not depend on, or roll back with, the receive.

The outbox record stores the operations. When the outbox detects that a message was already processed, it rebuilds the operations from that record and dispatches them again with `DispatchConsistency.Isolated`. A transport must therefore be able to send an operation using only its `OutgoingMessage` and `DispatchProperties`, with no state from the original dispatch call.

The outbox requires `TransportTransactionMode.ReceiveOnly`, and core refuses to start otherwise.
