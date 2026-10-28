# Message dispatchers

Transports implement `IMessageDispatcher` to send messages for core. Core registers `TransportInfrastructure.Dispatcher` as a singleton, so one dispatcher instance serves sends, publishes, replies, recoverability, and subscriptions for the lifetime of the endpoint.

Core does not initialize, start, or stop the dispatcher. It must be ready to dispatch when `TransportDefinition.Initialize` returns and keep working until `TransportInfrastructure.Shutdown` is called. This includes the time while receivers are stopping, because `StopReceive` waits for in-flight `OnMessage` and `OnError` invocations that may still dispatch.

A transport must support concurrent calls to `Dispatch`, because several messages can be processed at once and sends can also happen outside message processing.

## Dispatch paths

A transport that adds outgoing pipeline behaviors sees only the dispatches that pass through their stage:

- Sends, publishes, and replies pass through every outgoing stage.
- Recoverability on the main receiver passes through the routing and dispatch stages, but not the logical message stage.
- Operations replayed by the outbox pass through the batch dispatch and dispatch stages only.
- Satellite recoverability calls `Dispatch` directly, without any pipeline.

One `Send` or `Publish` call can resolve to several `TransportOperation`s. Each of those operations gets its own copy of the headers and dispatch properties, so a transport can mutate one without affecting the others. When the call resolves to a single operation, core does not copy them.

### Batching

Core does not limit the size of a batch, split it, or preserve the order in which a handler raised its operations. A transport whose broker limits how many operations it can send together must split the batch itself or reject it. A transport that must send operations in order has to record the order itself, for example in a `DispatchProperties` key set by a pipeline behavior; the batch currently arrives newest first.

While a handler processes an incoming message, core collects the operations raised with `DispatchConsistency.Default` in a batch for that message. When the handler pipeline completes, core passes the whole batch to one `Dispatch` call before the receive is completed. A transport can treat the operations passed to one call as eligible to be sent together.

Operations reach `Dispatch` immediately, outside any batch, when:

- The caller used `RequireImmediateDispatch()`, which sets `DispatchConsistency.Isolated` on the operation.
- The send, publish, or reply is made outside of handling an incoming message, for example from a timer or a hosted service.

Dispatches from recoverability are never batched either, because they do not run inside a message's handler pipeline.

## Multicast operations

Core produces a `MulticastTransportOperation` only when `TransportDefinition.SupportsPublishSubscribe` is `true`. [Message-driven publish/subscribe](#message-driven-publishsubscribe) describes what core sends otherwise, and during subscription migration mode. Core does not define what happens when a multicast event has no subscribers or its native topic does not exist; the transport may skip the publish or throw.

## Dispatch consistency

A transport must send an `Isolated` operation outside any ongoing receive transaction, in every transaction mode, so it reaches its destination even when the attempt that produced it later fails.

A transport must read `RequiredDispatchConsistency` per operation, because one `Dispatch` call can mix `Default` and `Isolated` operations. Batches that core assembles are uniform today, but a caller that uses the transport without an endpoint can mix them.

For `Default` operations dispatched while a message is being processed, `TransportTransactionMode` sets the atomicity rules:

- In `TransportTransactionMode.SendsAtomicWithReceive` and `TransportTransactionMode.TransactionScope`, the operations must commit atomically with completing the receive. When the attempt fails, they are rolled back with it and must never reach their destination.
- In `TransportTransactionMode.ReceiveOnly` and `TransportTransactionMode.None`, sends are not atomic with the receive. An operation can reach its destination even when completing the receive later fails.

[Dispatching from recoverability](recoverability.md#dispatching-from-recoverability) describes how these rules apply to dispatches made from `OnError`.

The `TransportTransaction` passed to `Dispatch` is usually the current receive's instance, or an empty one when there is no receive. A transport may offer a send option that lets the caller supply its own transaction, as SQL Server does with `UseCustomSqlTransaction`. The option should also require immediate dispatch, because core passes a caller-supplied `TransportTransaction` to `Dispatch` only for operations that are not batched. The `Isolated` rule applies to the receive transaction, so the transport may send the operation in the caller's transaction.

## Delivery properties

Core does not prevent one operation from carrying both a delay (`DoNotDeliverBefore` or `DelayDeliveryWith`) and `DiscardIfNotReceivedBefore`. The transport decides what that combination means and may reject it.

A transport must be able to send an operation using only its `OutgoingMessage` and `DispatchProperties`. Core can dispatch an operation again later from stored data, for example when the outbox finds a message that was already processed. The replay skips the earlier pipeline stages, as listed under [Dispatch paths](#dispatch-paths), and passes a different `TransportTransaction`. Keep anything the dispatcher needs in `DispatchProperties` as string values, not in the `TransportTransaction` or other in-memory state.

A transport may add its own keys to `DispatchProperties` to carry transport-specific data from a receive to a later dispatch of the same message, for example to recreate a native property on a delayed retry. When the outgoing message ID matches the incoming one, core copies each [`ReceiveProperties`](recoverability.md#reporting-a-failure) entry into `DispatchProperties` unless the key is already set. This covers audit, error, and retry dispatches.

## Message-driven publish/subscribe

This section applies only to transports that set `TransportDefinition.SupportsPublishSubscribe` to `false` or that support subscription migration mode.

Without native publish/subscribe, core turns a publish into one `UnicastTransportOperation` per known subscriber. To subscribe or unsubscribe, it sends a `Subscribe` or `Unsubscribe` control message to the publisher as a unicast operation. Core dispatches these control messages directly, without any pipeline, and never batches them. See [Subscriptions](infrastructure-setup.md#subscriptions).

In subscription migration mode, core does both. It sends the control messages, and one publish becomes a `MulticastTransportOperation` plus one `UnicastTransportOperation` per message-driven subscriber, all with the same message ID and in the same `Dispatch` call. A subscriber can be reachable both ways, so the transport must make sure it receives the event once, for example by skipping a unicast operation whose destination already receives the multicast one.
