# Transport receivers

Core drives every transport through `IMessageReceiver`. Transport authors should use the tests in `src/NServiceBus.TransportTests` as the executable contract.

## Call sequence

`ReceiveComponent` creates one receiver for the main input queue, one for the instance-specific queue when it is enabled, and one per satellite. For each receiver, core calls:

1. `Initialize` once, with the `PushRuntimeSettings`, the `OnMessage` delegate, and the `OnError` delegate.
2. `StartReceive` once, when the endpoint starts.
3. `ChangeConcurrency` any number of times while the receiver runs, only when consecutive-failure rate limiting is configured. See [Changing concurrency](#changing-concurrency).
4. `StopReceive` once, when the endpoint stops. Core passes the endpoint's stop token. Under the generic host, this is the token that `IHostedService.StopAsync` receives, which is canceled when the host's shutdown timeout expires. When an endpoint is disposed without being stopped, core passes a token that is already canceled.

After every receiver has stopped, core stops features and then calls `TransportInfrastructure.Shutdown`. Most transports call `StopReceive` on every receiver again from `Shutdown`, including the Learning transport, RabbitMQ, SQL Server, PostgreSQL, Azure Service Bus, Amazon SQS, and Azure Storage Queues. `StopReceive` must therefore be safe to call on a receiver that has already stopped. The TransportTests teardown stops the receiver and then calls `Shutdown`, so every test exercises this. When a transport is used without an endpoint, the caller makes these calls itself and should stop every receiver before it calls `Shutdown`.

A receiver must support `StartReceive` after `StopReceive`. The stop-and-restart approach to `ChangeConcurrency` depends on it.

## Concurrency

For the user-facing side of concurrency, see [Tuning endpoint message processing](https://docs.particular.net/nservicebus/operations/tuning).

`PushRuntimeSettings.MaxConcurrency` is "the maximum number of messages that should be in flight at any given time". In practice, it limits how many `OnMessage` and `OnError` invocations run at the same time.

- The limit applies per receiver, not per endpoint. The main and instance-specific receivers each get the same `PushRuntimeSettings`. Satellites get their own settings.
- A transport that reads from more than one native source for a single receiver must enforce one limit across all of them. Examples are a subscription per event type or several partitions. Giving each source its own limit multiplies the effective concurrency.
- Without `LimitMessageProcessingConcurrencyTo`, core passes the shared `PushRuntimeSettings.Default` instance. Its value is `Math.Max(2, Environment.ProcessorCount)`. A transport may compare against that instance to choose its own default. The Learning transport uses 1 in that case.
- When rate limiting ends, core passes the original instance to `ChangeConcurrency`. A transport that maps `PushRuntimeSettings.Default` to its own default must apply the same mapping there. The Learning transport does not, so after rate limiting ends it runs at `PushRuntimeSettings.Default.MaxConcurrency` instead of 1.
- Core provides no concurrency limiter. Each transport bounds concurrency itself, usually with a `SemaphoreSlim`, a client library setting, or a fixed number of consumers.
- Prefetching is allowed, but prefetched messages must not be passed to `OnMessage` beyond the limit. A concurrency change may temporarily exceed the new limit while existing invocations finish. See [Changing concurrency](#changing-concurrency). When messages wait for a slot, their native lease keeps running. See [Long-running handlers](#long-running-handlers).

The concurrency tests block every `OnMessage` invocation and check that the number of blocked invocations equals the limit and that every message is processed. They take the count as soon as it reaches the limit, so they can miss a receiver that briefly exceeds it.

### Changing concurrency

NServiceBus does not expose an endpoint API for users to change concurrency at runtime. Core uses `ChangeConcurrency` only for automatic rate limiting after consecutive processing failures.

[`WrappedMessageReceiver`](https://github.com/Particular/NServiceBus/pull/6214) calls `ChangeConcurrency` with `new PushRuntimeSettings(1)` when the consecutive-failure circuit breaker trips. It calls `ChangeConcurrency` with the original settings when the breaker resets. It makes these calls from its own background loop, not from inside `OnMessage` or `OnError`, so a receiver may wait for in-flight messages without deadlocking. When the receiver stops, the wrapper cancels the token that it passed to `ChangeConcurrency` and waits for the call to return before it calls `StopReceive`. The two calls never overlap. If a call throws, the wrapper logs a warning and does not retry. Core makes no further call until the breaker changes state again, so the receiver stays in whatever state the failed call left it.

A receiver must accept `ChangeConcurrency` at any time between `StartReceive` and `StopReceive`, while messages are in flight. The receiver must eventually enforce the new limit, but the call need not wait for existing invocations to finish. When lowering concurrency, existing `OnMessage` and `OnError` invocations may complete normally, temporarily exceeding the new limit even after the call returns. Changing concurrency must not cancel those invocations solely to meet the new limit.

Existing transports use one of three approaches:

| Approach | Transports | Trade-off |
| --- | --- | --- |
| Stop, drain, and restart | Learning, RabbitMQ, Amazon SQS, Azure Storage Queues, Non-Durable | The call waits for every in-flight message, and RabbitMQ also opens a new connection and releases its prefetched messages. |
| Replace the limiter | SQL Server, PostgreSQL, MSMQ | No restart. The call waits for messages admitted under the old limiter. Until they complete, the old and new limits can add up. |
| Resize in place | Azure Service Bus (`ServiceBusProcessor.UpdateConcurrency`), IBM MQ (add or stop workers) | No restart. Depends on the client library or on per-worker connections. When IBM MQ lowers concurrency, the call waits for the removed workers to stop. Azure Service Bus returns without waiting, so handlers above the new limit keep running after the call returns. |

The TransportTests change concurrency only before messages are sent. They do not exercise a change while handlers are running.

## Stopping and cancellation

`StopReceive` must stop receiving new messages and then wait for every in-flight `OnMessage` and `OnError` invocation to return. The token passed to `StopReceive` does not shorten that wait. Instead, the receiver forwards its cancellation to the token that it passes to `OnMessage` and `OnError`, so that handlers can cancel cooperatively. Handlers that ignore cancellation can delay shutdown indefinitely.

The token that a receiver passes to `OnMessage` and `OnError` should be canceled only when the receiver is stopping. Stopping without cancellation must let in-flight invocations complete without canceling their tokens. Canceling the stop token must cancel the tokens passed to those invocations.

An `OnMessage` invocation that fails because of that cancellation must not invoke `OnError`. An `OnError` invocation that fails because of that cancellation must not raise a critical error.

A client library with its own shutdown timeout can break this contract. An example is a library that abandons or negatively acknowledges in-flight messages after a fixed timeout. In that case, a message whose handler completes successfully is still redelivered.

## Long-running handlers

Core imposes no limit on how long `OnMessage` may run, and it does not cancel a handler for taking too long. Any limit comes from the transport: a peek lock, a visibility timeout, a broker acknowledgement timeout, or a transaction timeout. When that limit expires while a handler is still running, the message becomes available again, and another receiver can process it at the same time. The first handler's acknowledgement then fails or has no effect.

Existing transports deal with this in three ways:

- Renew the lease while the handler runs, up to a configurable cap. Azure Service Bus uses `MaxAutoLockRenewalDuration`, with a 5-minute default from the Azure SDK. Amazon SQS uses `MaxAutoMessageVisibilityRenewalDuration`, also with a 5-minute default, and `TimeSpan.Zero` disables renewal. Both expose the cap as a public transport setting.
- Detect the expiry when completing the message. Azure Storage Queues has no renewal, and `MessageInvisibleTime` defaults to 30 seconds. It raises a lease timeout when completion fails. RabbitMQ relies on the broker's `consumer_timeout`. It recognizes the acknowledgement-timeout channel closure and logs an error that tells the user to raise the timeout.
- Hold a transaction for the duration of the handler. SQL Server, PostgreSQL, and MSMQ have no lease. In `TransactionScope` mode, the scope timeout limits the handler, with a 1-minute default and a 10-minute machine-wide maximum. The native transaction modes have no time limit, but they hold a connection, a transaction, and locks for the duration of the handler. IBM MQ holds a syncpoint unit of work, and the transport sets no limit of its own.

When an acknowledgement fails after a handler has succeeded, RabbitMQ, Amazon SQS, Azure Storage Queues, and Azure Service Bus (in `ReceiveOnly` mode) record the native message ID in an in-memory LRU cache with 1,000 entries. When the message is redelivered to the same process, they complete it without invoking `OnMessage` again. This prevents duplicate processing only within one process.

Amazon SQS cancels the handler's token when lease renewal fails because the receipt handle is no longer valid, for example because the visibility timeout expired and another consumer received the message. The other transports do not cancel a handler when its lease is lost. The handler's token stays reserved for stopping.

When a transport has a native lease, it should:

- Renew the lease for every message that it has received and not yet completed, including messages that wait for a concurrency slot.
- Expose the renewal cap as a setting and document how it relates to handler duration.
- Log when renewal stops, when the cap is reached, or when completion fails because the lease expired.
