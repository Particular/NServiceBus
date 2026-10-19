# Transport receivers

Core drives every transport through `IMessageReceiver`. Transport authors should use the tests in `src/NServiceBus.TransportTests` as the executable contract.

## Call sequence

`ReceiveComponent` creates one receiver for the main input queue, one for the instance-specific queue when it is enabled, and one per satellite. For each receiver, core calls:

1. `Initialize` once, with the `PushRuntimeSettings`, the `OnMessage` delegate, and the `OnError` delegate.
2. `StartReceive` once, when the endpoint starts.
3. `ChangeConcurrency` any number of times while the receiver runs, only when consecutive-failure rate limiting is configured. See [Changing concurrency](#changing-concurrency).
4. `StopReceive` once, when the endpoint stops. Core passes the endpoint's stop token. Under the generic host, this is the token that `IHostedService.StopAsync` receives, which is canceled when the host's shutdown timeout expires. When an endpoint is disposed without being stopped, core passes a token that is already canceled.

After every receiver has stopped, core stops features and then calls `TransportInfrastructure.Shutdown`. `Shutdown` may call `StopReceive` again, so `StopReceive` must be safe to call on a receiver that has already stopped. When a transport is used without an endpoint, the caller should stop every receiver before calling `Shutdown`.

A receiver must support `StartReceive` after `StopReceive`.

## Concurrency

For the user-facing side of concurrency, see [Tuning endpoint message processing](https://docs.particular.net/nservicebus/operations/tuning).

`PushRuntimeSettings.MaxConcurrency` is "the maximum number of messages that should be in flight at any given time". In practice, it limits how many `OnMessage` and `OnError` invocations run at the same time.

- The limit applies per receiver, not per endpoint. The main and instance-specific receivers each get the same `PushRuntimeSettings`. Satellites get their own settings.
- A transport that reads from more than one native source for a single receiver must enforce one limit across all of them. Examples are a subscription per event type or several partitions. Giving each source its own limit multiplies the effective concurrency.
- Without `LimitMessageProcessingConcurrencyTo`, core passes the shared `PushRuntimeSettings.Default` instance. Its value is `Math.Max(2, Environment.ProcessorCount)`. A transport may compare against that instance to choose its own default.
- When rate limiting ends, core passes the original instance to `ChangeConcurrency`. A transport that maps `PushRuntimeSettings.Default` to its own default must apply the same mapping there.
- Core provides no concurrency limiter. Each transport must enforce the limit.
- Prefetching is allowed, but prefetched messages must not be passed to `OnMessage` beyond the limit. A concurrency change may temporarily exceed the new limit while existing invocations finish. See [Changing concurrency](#changing-concurrency). When messages wait for a slot, their native lease keeps running. See [Long-running handlers](#long-running-handlers).

### Changing concurrency

NServiceBus does not expose an endpoint API for users to change concurrency at runtime. Core uses `ChangeConcurrency` only for [automatic rate limiting](https://docs.particular.net/nservicebus/recoverability/#automatic-rate-limiting).

Core calls `ChangeConcurrency` outside `OnMessage` and `OnError`, so a receiver may wait for in-flight messages without deadlocking. During shutdown, core cancels the token passed to `ChangeConcurrency` and waits for the call to return before calling `StopReceive`.

A receiver must accept `ChangeConcurrency` at any time between `StartReceive` and `StopReceive`, while messages are in flight. The receiver must eventually enforce the new limit, but the call need not wait for existing invocations to finish. When lowering concurrency, existing `OnMessage` and `OnError` invocations may complete normally, temporarily exceeding the new limit even after the call returns. Changing concurrency must not cancel those invocations solely to meet the new limit.

## Stopping and cancellation

`StopReceive` must stop receiving new messages and wait for all in-flight `OnMessage` and `OnError` invocations to return.

- If the stop token is not canceled, let those invocations finish without canceling their processing tokens.
- If the stop token is canceled, cancel their processing tokens so handlers can stop cooperatively. This also applies when the stop token is already canceled when `StopReceive` is called.

Cancellation requests that handlers stop; it does not end the wait. Handlers that ignore cancellation can delay shutdown indefinitely. A client library's shutdown timeout must not cause `StopReceive` to return while invocations are still running.

Do not invoke `OnError` when `OnMessage` throws because of shutdown cancellation. Do not raise a critical error when `OnError` throws because of shutdown cancellation. See [Cancellation and catching exceptions](https://docs.particular.net/nservicebus/cancellation-and-catching-exceptions) for how to distinguish cancellation from a processing failure.

## Long-running handlers

Core imposes no limit on how long `OnMessage` may run, and it does not cancel a handler for taking too long. Any limit comes from the transport: a peek lock, a visibility timeout, a broker acknowledgement timeout, or a transaction timeout. When that limit expires while a handler is still running, the message becomes available again, and another receiver can process it at the same time. The first handler's acknowledgement then fails or has no effect.

A transport may remember successfully processed messages whose acknowledgement failed, for example because their lease expired. On redelivery, it can retry acknowledgement without invoking `OnMessage` again. This is an optional mitigation for duplicate processing, not a core requirement. Its protection depends on how long the transport retains that information and whether it is available to the receiver handling the redelivery. It does not imply a durable deduplication guarantee.

When a transport has a native lease, it should:

- Renew the lease for every message that it has received and not yet completed, including messages that wait for a concurrency slot.
- Expose the renewal cap as a setting and document how it relates to handler duration.
- Log when renewal stops, when the cap is reached, or when completion fails because the lease expired.
