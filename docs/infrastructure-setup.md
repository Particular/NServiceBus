# Infrastructure setup

`HostSettings.SetupInfrastructure` tells a transport whether to create infrastructure. When an endpoint hosts the transport, the flag is `true` if the endpoint configuration calls `EnableInstallers()` or the host calls `AddNServiceBusInstallers()`. The `receivers` and `sendingAddresses` arguments of `TransportDefinition.Initialize` list the queues that core expects it to create when the flag is `true`. This page describes what core puts in these inputs and what it expects the transport to create. For the user-facing side, see [Installers](https://docs.particular.net/nservicebus/operations/installers).

## Native resources

Core distinguishes three kinds of native resources:

- Endpoint infrastructure: the queues the endpoint receives from. Core passes the main, instance-specific, and satellite queues in `receivers`.
- Shared infrastructure: the [sending addresses](#sending-addresses), which are usually the system-wide error queue and audit queue. Several endpoints use them and no single endpoint owns them.
- Publish/subscribe topology: the resources that carry an event from a publisher to its subscribers; see [Publish/subscribe topology](#publishsubscribe-topology).

Each transport must document which of its native resources belong to each kind, and the permissions that publishing, subscribing, and unsubscribing need.

## Sending addresses

Core passes `QueueBindings.SendingAddresses` to `Initialize` as `sendingAddresses`. The array contains only the addresses that were registered with `QueueBindings.BindSending`. Core registers two:

- The error queue, for every endpoint that is not send-only.
- The audit queue, when auditing is enabled and the endpoint is not send-only.

Features outside core can also call `BindSending`.

The array does not contain:

- Receive addresses, which core passes in `receivers`. Each `ReceiveSettings.ErrorQueue` also contains the error queue.
- Destinations of commands, replies, or other routed messages. A sender does not create the input queues of the endpoints it sends to.

### Resolved addresses

The entries in `sendingAddresses` and each `ReceiveSettings.ErrorQueue` are transport addresses. Core uses them as given and never passes them through `TransportInfrastructure.ToTransportAddress`.

Core sends failed and audited messages to these transport addresses. A transport that changes names, for example by adding a prefix or a namespace, must apply that change to these addresses itself: when it creates the queues, when its dispatcher sends to them, and when it moves a message to `ReceiveSettings.ErrorQueue` without going through core. This keeps the queue the installer creates the same as the queue that receives failed and audited messages.

The dispatcher cannot tell whether core resolved a destination through `ToTransportAddress`:

- Resolved: a command routed by message type, an event sent to a message-driven subscriber, a reply to the sender's default `ReplyTo` address, and a delayed retry.
- Used as given: failed and audited messages, and addresses passed to `SetDestination`, `RouteReplyTo`, `OverridePublicReturnAddress`, or `ForwardCurrentMessageTo`.

The dispatcher must therefore not apply the change a second time to an address that already includes it.

## Publish/subscribe topology

The publish/subscribe topology is every native resource that an event passes through between a publisher and its subscribers. It has two parts:

- Event topics, on the publish side: the resources that a publisher dispatches an event through, such as a topic, an exchange and the exchanges of the event's base types, or a subscription table. An event has one logical publisher, which can run as several endpoint instances, and its event topic belongs to that publisher. The event's subscribers use the topic too; see [Event topics](#event-topics).
- Subscriptions, on the subscribe side: the resources that connect one event topic to one subscriber, such as a native subscription, a binding, a filter rule, a subscription row, or a queue policy that lets a topic deliver to the subscriber's queue. A subscription belongs to the subscribing endpoint; see [Subscriptions](#subscriptions).

The publisher owns an event topic, but a subscriber usually creates it, because core passes event types to `SubscribeAll` and not to `Initialize`.

### Event topics

Core does not tell a transport which event types an endpoint publishes. `Initialize` receives only queues, and an endpoint can publish any event type at runtime, so no complete list exists at startup. A transport that maps event types to native topics or exchanges learns about event types in three places:

- In a subscribing endpoint, `SubscribeAll` receives the event types that the endpoint handles. When `SetupInfrastructure` is `true`, the transport creates the topics that the subscription needs here.
- In a publishing endpoint, the dispatcher receives the event type in each `MulticastTransportOperation`, when the endpoint publishes the event.
- In either endpoint, the transport's own topology configuration, such as a `PublishTo` mapping in a publisher or a `SubscribeTo` mapping in a subscriber. A mapping covers only the event types that the user configured, so it does not replace subscribing as the way to create the other topics.

Without a mapping, a publishing endpoint learns an event type only when it publishes it, while a subscribing endpoint learns it when it subscribes: at startup through `AutoSubscribe` for the events it handles, or later through `IMessageSession.Subscribe`; see [Subscriptions](#subscriptions).

A publisher can publish an event before any subscriber has created its topic.

### Subscriptions

Core runs subscription operations independently of installers. It calls `ISubscriptionManager` whether or not `SetupInfrastructure` is set:

- The `AutoSubscribe` feature is enabled by default. Each time an endpoint that is not send-only starts, it calls `ISubscriptionManager.SubscribeAll` with the event types the endpoint handles. A failure does not stop the endpoint from starting; see [Failures in `SubscribeAll`](#failures-in-subscribeall).
- `Subscribe` and `Unsubscribe` calls from `IMessageSession` reach `ISubscriptionManager`, also in subscription migration mode. A `Subscribe` call reaches the transport as `SubscribeAll` with one event type.

A subscription lasts until `Unsubscribe` removes it. It survives endpoint restarts, and the endpoint receives the events published to it whether or not the current run subscribes again. Scripts, infrastructure-as-code, or earlier runs may have created subscriptions that the current run never requests. An endpoint that disables `AutoSubscribe` usually relies on subscriptions created this way, so core might not call `ISubscriptionManager.SubscribeAll` during a run. A transport must therefore treat the native subscriptions that exist for an endpoint as authoritative. The event types passed to `SubscribeAll` are not the endpoint's complete set of subscriptions, so a transport must not delete a subscription because it is missing from that list or because the transport does not recognize it.

When the broker or the publisher delivers events to the endpoint's input queue, receiving from the input queue is enough. When the transport receives from each native subscription separately, the receiver must find the endpoint's existing subscriptions itself; see [Message receivers](message-receivers.md#subscriptions).

Running only the installers, for example with `AddNServiceBusInstallers()`, does not start the endpoint, so core does not call `SubscribeAll`. It creates no subscriptions, and no event topics that the transport creates when subscribing; see [Event topics](#event-topics).

`SubscribeAll` creates the endpoint's subscriptions whether or not `SetupInfrastructure` is set. It creates the event topics that a subscription needs only when `SetupInfrastructure` is `true`. Otherwise they must already exist, and `SubscribeAll` throws when they do not; see [Failures in `SubscribeAll`](#failures-in-subscribeall).

`Unsubscribe` deletes the endpoint's subscription for the event type whether or not `SetupInfrastructure` is set. It does not delete event topics, because other endpoints may use them.

#### Failures in `SubscribeAll`

A transport should throw when it cannot establish the requested subscription, so that core can decide what the failure means for the caller:

- `AutoSubscribe` logs the exception at `Error`, and the endpoint keeps starting. For an `AggregateException`, it logs each inner exception.
- For a `Subscribe` call from `IMessageSession`, core rethrows the exception to the caller. It unwraps an `AggregateException` to its first inner exception.

Core does not retry a failed subscription. With `AutoSubscribe`, the next attempt happens when the endpoint starts again.

> [!NOTE]
> An `ISubscriptionManager` should not throw `QueueNotFoundException`. Core reserves it for message-driven publish/subscribe, where `AutoSubscribe` rethrows it so that a missing publisher queue stops the endpoint from starting.

#### Failures in `Unsubscribe`

`Unsubscribe` should succeed when the subscription does not exist. When it cannot remove the subscription for any other reason, including missing rights, the transport should throw. It may retry a transient failure first. Core passes the exception to the caller of `IMessageSession.Unsubscribe`, which otherwise cannot tell that the endpoint still receives the event.
