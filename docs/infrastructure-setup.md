# Infrastructure setup

`TransportDefinition.Initialize` receives two inputs that decide what a transport creates: the `HostSettings.SetupInfrastructure` flag and the `sendingAddresses` array. This page describes what core puts in each and what it expects the transport to create. For the user-facing side, see [Installers](https://docs.particular.net/nservicebus/operations/installers).

## SetupInfrastructure

`HostSettings.SetupInfrastructure` is `true` when the endpoint configuration calls `EnableInstallers()`, and `false` otherwise. Core reads the setting when it creates the endpoint and passes the flag to the transport. Core does not read the flag anywhere else, so the transport decides what to create when it is set.

- `Installer.Setup` and `Installer.CreateInstallerWithExternallyManagedContainer` call `EnableInstallers()`, so the transport gets `true`.
- `AddNServiceBusInstallers()` runs the endpoint's installers without calling `EnableInstallers()`. When the endpoint configuration does not call `EnableInstallers()` either, the transport gets `false`.
- When a transport runs without an endpoint, the caller sets the flag through the `HostSettings` constructor.

The flag covers the queues described below. It does not cover subscriptions; see [Subscriptions](#subscriptions).

## Sending addresses

Core passes `QueueBindings.SendingAddresses` to `Initialize` as `sendingAddresses`. The array contains only the addresses that were registered with `QueueBindings.BindSending`. Core registers two:

- The error queue, for every endpoint that is not send-only.
- The audit queue, when auditing is configured with `AuditProcessedMessagesTo` or the `NSERVICEBUS__AUDIT__ADDRESS` environment variable and the endpoint is not send-only.

Features outside core can also call `BindSending`.

The array does not contain:

- Receive addresses. Core passes the main, instance-specific, and satellite queues in `receivers`. Each `ReceiveSettings.ErrorQueue` also contains the error queue.
- Destinations of commands, replies, or other routed messages. A sender does not create the input queues of the endpoints it sends to.
- The ServiceControl queues that the Heartbeat, CustomChecks, SagaAudit, and Metrics.ServiceControl packages send to. These packages dispatch without calling `BindSending`, so installers do not create those queues on any transport.

## Creating queues for sending addresses

When `SetupInfrastructure` is `true`, a transport should create a complete queue for each sending address. A complete queue includes every native resource the transport needs to accept a message for that address and keep it until a receiver reads it. Error and audit messages then stay in their queues until a consumer, such as ServiceControl, starts.

A queue that has only some of those resources can accept a send and lose the message without an error. For example, a broker can discard a message published to a topic that has no subscriptions.

## Subscriptions

Core runs subscription operations independently of installers. It calls `ISubscriptionManager` whether or not `SetupInfrastructure` is set:

- The `AutoSubscribe` feature is enabled by default. Each time an endpoint that is not send-only starts, it calls `ISubscriptionManager.SubscribeAll` with the event types the endpoint handles. Only a `QueueNotFoundException` stops the endpoint from starting; see [Failures in `SubscribeAll`](#failures-in-subscribeall).
- `NativeSubscribeTerminator` and `NativeUnsubscribeTerminator` pass `Subscribe` and `Unsubscribe` calls from `IMessageSession` to `ISubscriptionManager`. A `Subscribe` call reaches the transport as `SubscribeAll` with one event type. In subscription migration mode, `MigrationSubscribeTerminator` and `MigrationUnsubscribeTerminator` call it as well.

A subscription lasts until `Unsubscribe` removes it. It survives endpoint restarts, and the endpoint receives the events published to it whether or not the current run subscribes again. The user-facing documentation states this for [stopped subscribers and decommissioned handlers](https://docs.particular.net/nservicebus/messaging/publish-subscribe/controlling-what-is-subscribed). Scripts, infrastructure-as-code, or earlier runs may have created subscriptions that the current run never requests. An endpoint that disables `AutoSubscribe` usually relies on subscriptions created this way, so core might not call `ISubscriptionManager.SubscribeAll` during a run. A transport must therefore treat the native subscriptions that exist for an endpoint as authoritative. The event types passed to `SubscribeAll` are not the endpoint's complete set of subscriptions, so a transport must not delete a subscription because it is missing from that list or because the transport does not recognize it.

When the broker or the publisher delivers events to the endpoint's input queue, receiving from the input queue is enough. When the transport receives from each native subscription separately, the receiver must find the endpoint's existing subscriptions itself; see [Message receivers](message-receivers.md#subscriptions).

Running only the installers, for example with `AddNServiceBusInstallers()`, does not start the endpoint, so it does not create subscriptions.

Calling `SubscribeAll` independently of installers does not mean that every resource needed for subscribing must be created independently of installers. A transport may create the endpoint’s native subscription while requiring a shared topic to exist when `SetupInfrastructure` is `false`. Core does not prescribe that resource boundary.

Each transport must document which resources subscribing creates with installers enabled and disabled, which resources deployment must provide, and the permissions required in each case. If a required resource is missing, `SubscribeAll` should throw; see [Failures in `SubscribeAll`](#failures-in-subscribeall).

Core also does not define which native resources `Unsubscribe` may delete when `SetupInfrastructure` is `false`.

The user-facing documentation also says that [auto-subscribe is not part of installers](https://docs.particular.net/nservicebus/operations/installers#auto-subscribe-is-not-part-of-installers).

### Failures in `SubscribeAll`

A transport should throw when it cannot establish the requested subscription instead of logging or swallowing the failure. Core decides what the failure means for the caller:

- `AutoSubscribe` logs the exception at `Error`, and the endpoint keeps starting. For an `AggregateException`, it logs each inner exception. It rethrows a `QueueNotFoundException`, including one inside an `AggregateException`, and the endpoint does not start.
- For a `Subscribe` call from `IMessageSession`, `NativeSubscribeTerminator` rethrows the exception to the caller. It unwraps an `AggregateException` to its first inner exception. A `Subscribe` call in a feature startup task or a hosted service fails startup only if that code lets the exception escape.

Core does not retry a failed subscription. When the transport swallows a failure, the endpoint runs without the subscription and nothing is logged.

Core does not require a transport to attempt every event type before it throws. A transport that stops at the first failure leaves the remaining event types in that call unsubscribed, and `AutoSubscribe` logs only the exception it receives.

A transport should throw `QueueNotFoundException` only when a missing queue should stop the endpoint from starting. When the token passed to `SubscribeAll` is canceled, the transport should throw `OperationCanceledException` without wrapping it. When startup is canceled, `AutoSubscribe` lets that exception propagate. It logs a wrapped one as a failed subscription.
