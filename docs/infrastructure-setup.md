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

A queue that has only some of those resources can accept a send and lose the message without an error. Google Pub/Sub, for example, discards a message published to a topic that has no subscriptions. Before [NServiceBus.Transport.GooglePubSub#30](https://github.com/Particular/NServiceBus.Transport.GooglePubSub/pull/30), that transport created only a topic for each sending address, so audit messages were lost until something else created the subscription.

The SQS, Azure Service Bus, RabbitMQ, SQL Server, PostgreSQL, MSMQ, and Google Pub/Sub transports create a complete queue for each sending address. The RabbitMQ conventional routing topology, for example, declares the queue, an exchange with the same name, and the binding between them.

## Subscriptions

Subscriptions are not part of installers. Core calls `ISubscriptionManager` whether or not `SetupInfrastructure` is set:

- The `AutoSubscribe` feature is enabled by default. It calls `SubscribeAll` every time an endpoint that is not send-only starts. When `SubscribeAll` throws, `AutoSubscribe` logs the exception at `Error` and the endpoint keeps starting. It rethrows only a `QueueNotFoundException`.
- `NativeSubscribeTerminator` and `NativeUnsubscribeTerminator` pass `Subscribe` and `Unsubscribe` calls from `IMessageSession` to `ISubscriptionManager`. In subscription migration mode, `MigrationSubscribeTerminator` and `MigrationUnsubscribeTerminator` call it as well.

Running only the installers, for example with `AddNServiceBusInstallers()`, does not start the endpoint, so it does not create subscriptions.

Core does not define which native resources `SubscribeAll` may create, or which resources `Unsubscribe` may delete, when `SetupInfrastructure` is `false`. Current transports handle it in different ways:

- SQS creates the SNS topic only when `SetupInfrastructure` is `true`. Otherwise it throws when the topic does not exist. It always creates the SNS subscription and always removes it on unsubscribe.
- Azure Service Bus, with the topic-per-event topology, creates the topic only when `SetupInfrastructure` is `true`. It always creates the subscription and its rules. On unsubscribe it always deletes the subscription, or the rule when the topic uses filter-based routing. When the credentials lack the required rights, creating a subscription throws, and deleting a subscription logs the failure at `Info`.
- RabbitMQ always declares the event exchanges and always binds the endpoint's queue or exchange to them. It always removes the binding on unsubscribe.
- Google Pub/Sub creates the topic and the subscription for an event only when `SetupInfrastructure` is `true`, and deletes the subscription on unsubscribe only then. Otherwise it expects them to exist and only starts or stops receiving from them.

The user-facing documentation also says that [auto-subscribe is not part of installers](https://docs.particular.net/nservicebus/operations/installers#auto-subscribe-is-not-part-of-installers).
