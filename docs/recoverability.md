# Recoverability

When `OnMessage` throws, the transport reports the failure to core through `OnError`, and core decides what happens to the message. The transport then carries out that decision. For the user-facing side, see [Recoverability](https://docs.particular.net/nservicebus/recoverability/). Transport authors should use the tests in `src/NServiceBus.TransportTests` as the executable contract. Failures caused by shutdown cancellation are covered in [Stopping and cancellation](message-receivers.md#stopping-and-cancellation).

## Reporting a failure

Call `OnError` once for each failed processing attempt, in every transaction mode. Core counts each `OnError` call as a failure, and each successful `OnMessage` as a success, for [automatic rate limiting](https://docs.particular.net/nservicebus/recoverability/#automatic-rate-limiting).

A failure to complete the receive after `OnMessage` returns is also a processing failure. An example is a `TransactionScope` that aborts when it commits.

Populate the `ErrorContext` as follows:

- `Exception`: the exception that caused the failure.
- `Headers`: the headers as received. `OnMessage` and `OnError` can both change the dictionary they get, so give each call its own copy. Changes made in either call must not reach `OnError` or a retry.
- `NativeMessageId` and `Body`: the values passed to `OnMessage`.
- `ReceiveAddress`: the receiver's address. Core sends delayed retries to it and writes it to the `NServiceBus.FailedQ` header, so the dispatcher must be able to send to it.
- `Extensions`: the `ContextBag` that was passed to `MessageContext`, so values set during processing reach recoverability.
- `ReceiveProperties`: optional. Except on satellite receivers, core copies them to the error queue copy and the delayed retry, because both keep the incoming message ID.
- `TransportTransaction`: see [Dispatching from recoverability](#dispatching-from-recoverability).
- `ImmediateProcessingFailures`: see [Counting failures](#counting-failures).

Core reads `DelayedDeliveriesPerformed` from the `NServiceBus.Retries` header. The transport does not track it.

## Counting failures

`ImmediateProcessingFailures` is the number of failed attempts to process this native message, including the current one. Pass 1 for the first failure. The default policy requests an immediate retry while the count is at most the configured number of immediate retries.

- Keep the count across immediate retries.
- Start a new count for each native message. Core sends a delayed retry as a new message with the same message ID, and its count must start at 1. A count keyed only on the NServiceBus message ID does not reset.
- A native delivery counter works across endpoint instances. If it also counts deliveries that did not fail, such as a redelivery after a lease expired, the message gets fewer immediate retries.
- An in-memory count is acceptable. It is lost on restart and not shared between instances, so a message can get more immediate retries than configured. Users are told that the configured number is a minimum; see [scale-out multiplier](https://docs.particular.net/nservicebus/recoverability/#scale-out-multiplier). Bound the memory the count uses.

## Acting on the result

`OnError` returns one of two values:

- `RetryRequired`: core did not handle the message. Make it available for another attempt. The next attempt gets the original headers. Of the built-in recoverability actions, only an immediate retry returns this, and core disables immediate retries in `TransportTransactionMode.None`.
- `Handled`: core moved the message to the error queue, scheduled a delayed retry, discarded it, or ran a custom recoverability action. Complete the receive. Treat the message as done even though no handler succeeded.

A transport may run an immediate retry in memory by calling `OnMessage` again without releasing the message. The attempts then add up against any native lease. See [Long-running handlers](message-receivers.md#long-running-handlers).

## Dispatching from recoverability

Core sends the error queue copy and the delayed retry through the transport's `IMessageDispatcher`. It uses `DispatchConsistency.Default` and passes `ErrorContext.TransportTransaction`. Code in `OnError`, such as a custom recoverability action, may dispatch the same way.

- In `TransportTransactionMode.SendsAtomicWithReceive` and `TransportTransactionMode.TransactionScope`, sends made from `OnError` must commit atomically with completing the receive when `OnError` returns `Handled`. Sends that `OnMessage` made before the failure must not be emitted.
- Sends that `OnMessage` makes with `DispatchConsistency.Isolated` are emitted even when processing fails.
- In `TransportTransactionMode.ReceiveOnly` and `TransportTransactionMode.None`, sends are not atomic with the receive. If completing the receive fails after `OnError` returned `Handled`, the error queue can get a duplicate.

If the failed attempt ran in a transaction, that transaction must be rolled back. The `TransportTransaction` passed to `OnError` must not include the failed attempt's work. When `OnError` returns `Handled`, commit the transaction passed to `OnError` together with completing the receive. When it returns `RetryRequired`, roll that transaction back.

Delayed retries require `TransportDefinition.SupportsDelayedDelivery`. When the transport does not support delayed delivery, core refuses to start with delayed retries configured, and moves the message to the error queue when a custom policy requests a delayed retry.

A transport can provide its own `RecoverabilityAction` that leaves a request in `TransportTransaction` for the receiver to act on after `OnError` returns.

## When OnError throws

When `OnError` throws for a reason other than shutdown cancellation:

1. Raise a critical error with the message ``Failed to execute recoverability policy for message with native ID: `{nativeMessageId}` `` and the exception from `OnError`.
2. Handle the message as if `OnError` had returned `RetryRequired`.

Do not log the failure above `Info` as well.

## Messages core cannot process

When a transport detects a message that it cannot turn into a `MessageContext`, the transport should move the message to the error queue or to the queuing system's native dead-letter queue. Examples are headers that cannot be deserialized, a message ID that cannot be determined, an envelope that cannot be unwrapped, or a body that cannot be retrieved.