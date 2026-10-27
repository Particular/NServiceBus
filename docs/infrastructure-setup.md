# Infrastructure setup

`HostSettings.SetupInfrastructure` tells a transport whether to create infrastructure. When an endpoint hosts the transport, the flag is `true` if the endpoint configuration calls `EnableInstallers()` or the host calls `AddNServiceBusInstallers()`. The `receivers` and `sendingAddresses` arguments of `TransportDefinition.Initialize` list the queues that core expects it to create. The transport decides what else it needs, based on how it maps queues and events to native resources. An example is a topic or exchange that several endpoints share. This page describes what core puts in these inputs and what it expects the transport to create. For the user-facing side, see [Installers](https://docs.particular.net/nservicebus/operations/installers).

## Native resources

Core distinguishes three kinds of native resources:

- Endpoint infrastructure: the queues the endpoint receives from. Core passes the main, instance-specific, and satellite queues in `receivers`. A transport creates them when `SetupInfrastructure` is `true`.
- Shared infrastructure: resources that several endpoints use and no single endpoint owns. Examples are the [sending addresses](#sending-addresses), which are usually the system-wide error queue and audit queue, and event topics, exchanges, or a subscription table. A transport creates them when `SetupInfrastructure` is `true`.
- Subscriptions: the resources that make one endpoint receive one event type, such as a native subscription, a binding, a filter rule, a subscription row, or a queue policy that lets a topic deliver to the endpoint's queue. A transport creates and deletes them whether or not `SetupInfrastructure` is set; see [Subscriptions](#subscriptions).

Each transport must document which of its native resources belong to each kind, and the permissions that subscribing and unsubscribing need.

## Sending addresses

Core passes `QueueBindings.SendingAddresses` to `Initialize` as `sendingAddresses`. The array contains only the addresses that were registered with `QueueBindings.BindSending`. Core registers two:

- The error queue, for every endpoint that is not send-only.
- The audit queue, when auditing is enabled and the endpoint is not send-only.

Features outside core can also call `BindSending`.

The array does not contain:

- Receive addresses, which core passes in `receivers`. Each `ReceiveSettings.ErrorQueue` also contains the error queue.
- Destinations of commands, replies, or other routed messages. A sender does not create the input queues of the endpoints it sends to.

## Subscriptions

Core runs subscription operations independently of installers. It calls `ISubscriptionManager` whether or not `SetupInfrastructure` is set:

- The `AutoSubscribe` feature is enabled by default. Each time an endpoint that is not send-only starts, it calls `ISubscriptionManager.SubscribeAll` with the event types the endpoint handles. A failure does not stop the endpoint from starting; see [Failures in `SubscribeAll`](#failures-in-subscribeall).
- `Subscribe` and `Unsubscribe` calls from `IMessageSession` reach `ISubscriptionManager`, also in subscription migration mode. A `Subscribe` call reaches the transport as `SubscribeAll` with one event type.

A subscription lasts until `Unsubscribe` removes it. It survives endpoint restarts, and the endpoint receives the events published to it whether or not the current run subscribes again. Scripts, infrastructure-as-code, or earlier runs may have created subscriptions that the current run never requests. An endpoint that disables `AutoSubscribe` usually relies on subscriptions created this way, so core might not call `ISubscriptionManager.SubscribeAll` during a run. A transport must therefore treat the native subscriptions that exist for an endpoint as authoritative. The event types passed to `SubscribeAll` are not the endpoint's complete set of subscriptions, so a transport must not delete a subscription because it is missing from that list or because the transport does not recognize it.

When the broker or the publisher delivers events to the endpoint's input queue, receiving from the input queue is enough. When the transport receives from each native subscription separately, the receiver must find the endpoint's existing subscriptions itself; see [Message receivers](message-receivers.md#subscriptions).

Running only the installers, for example with `AddNServiceBusInstallers()`, does not start the endpoint, so it does not create subscriptions.

`SubscribeAll` creates the endpoint's subscriptions whether or not `SetupInfrastructure` is set. It creates the shared infrastructure that a subscription needs only when `SetupInfrastructure` is `true`. Otherwise that infrastructure must already exist, and `SubscribeAll` throws when it does not; see [Failures in `SubscribeAll`](#failures-in-subscribeall).

`Unsubscribe` deletes the endpoint's subscription for the event type whether or not `SetupInfrastructure` is set. It does not delete shared infrastructure, because other endpoints may use it.

### Failures in `SubscribeAll`

A transport should throw when it cannot establish the requested subscription instead of logging or swallowing the failure. Core decides what the failure means for the caller:

- `AutoSubscribe` logs the exception at `Error`, and the endpoint keeps starting. For an `AggregateException`, it logs each inner exception.
- For a `Subscribe` call from `IMessageSession`, core rethrows the exception to the caller. It unwraps an `AggregateException` to its first inner exception.

Core does not retry a failed subscription. When the transport swallows a failure, the endpoint runs without the subscription and nothing is logged.

Core does not require a transport to attempt every event type before it throws. A transport that stops at the first failure leaves the remaining event types in that call unsubscribed, and `AutoSubscribe` logs only the exception it receives.

> [!NOTE]
> An `ISubscriptionManager` should not throw `QueueNotFoundException`. `AutoSubscribe` rethrows it, so the endpoint does not start. Core reserves it for message-driven publish/subscribe, where a subscribe message is sent to the publisher's queue and a missing queue should stop the endpoint.

### Failures in `Unsubscribe`

When `Unsubscribe` cannot remove the subscription, for example because the credentials lack the required rights, the transport should throw. Core passes the exception to the caller of `IMessageSession.Unsubscribe`. When the transport swallows the failure, the subscription remains and the endpoint keeps receiving the event.

When the subscription does not exist, `Unsubscribe` should succeed.
