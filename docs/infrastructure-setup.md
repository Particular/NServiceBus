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

Subscriptions are not part of installers. Core calls `ISubscriptionManager` whether or not `SetupInfrastructure` is set:

- The `AutoSubscribe` feature is enabled by default. It calls `SubscribeAll` every time an endpoint that is not send-only starts. When `SubscribeAll` throws, `AutoSubscribe` logs the exception at `Error` and the endpoint keeps starting. It rethrows only a `QueueNotFoundException`.
- `NativeSubscribeTerminator` and `NativeUnsubscribeTerminator` pass `Subscribe` and `Unsubscribe` calls from `IMessageSession` to `ISubscriptionManager`. In subscription migration mode, `MigrationSubscribeTerminator` and `MigrationUnsubscribeTerminator` call it as well.

A subscription lasts until `Unsubscribe` removes it. It survives endpoint restarts, and the endpoint receives the events published to it whether or not the current run calls `Subscribe`. The user-facing documentation states this for [stopped subscribers and decommissioned handlers](https://docs.particular.net/nservicebus/messaging/publish-subscribe/controlling-what-is-subscribed). Scripts, infrastructure-as-code, or earlier runs may have created subscriptions that the current run never requests. An endpoint that disables `AutoSubscribe` usually relies on subscriptions created this way, so core might not call `SubscribeAll` or `Subscribe` during a run. A transport must therefore treat the native subscriptions that exist for an endpoint as authoritative. It must not delete a subscription it does not recognize, such as one for an event that has no handler in the endpoint when `SubscribeAll` runs.

When the broker or the publisher delivers events to the endpoint's input queue, receiving from the input queue is enough. When the transport receives from each native subscription separately, the receiver must find the endpoint's existing subscriptions itself; see [Message receivers](message-receivers.md#subscriptions).

Running only the installers, for example with `AddNServiceBusInstallers()`, does not start the endpoint, so it does not create subscriptions.

Core does not define which native resources `SubscribeAll` may create, or which resources `Unsubscribe` may delete, when `SetupInfrastructure` is `false`.

The user-facing documentation also says that [auto-subscribe is not part of installers](https://docs.particular.net/nservicebus/operations/installers#auto-subscribe-is-not-part-of-installers).
