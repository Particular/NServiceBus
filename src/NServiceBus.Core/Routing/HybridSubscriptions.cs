namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Features;
    using Logging;
    using Pipeline;
    using Transport;
    using Unicast.Messages;
    using Unicast.Queuing;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Transport;

    class HybridSubscriptions : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");

            var distributionPolicy = context.Routing.DistributionPolicy;
            var publishers = context.Routing.Publishers;
            var configuredPublishers = context.Settings.Get<ConfiguredPublishers>();
            var conventions = context.Settings.Get<Conventions>();
            var enforceBestPractices = context.Routing.EnforceBestPractices;

            configuredPublishers.Apply(publishers, conventions, enforceBestPractices);

            context.Pipeline.Register(b =>
            {
                var unicastPublishRouter = new UnicastPublishRouter(b.Build<MessageMetadataRegistry>(), i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)), b.Build<ISubscriptionStorage>());
                return new HybridRouterConnector(distributionPolicy, unicastPublishRouter);
            }, "Determines how the published messages should be routed");

            if (canReceive)
            {
                var endpointInstances = context.Routing.EndpointInstances;
                var transportSubscriptionInfrastructure = transportInfrastructure.ConfigureSubscriptionInfrastructure();
                var subscriptionManager = transportSubscriptionInfrastructure.SubscriptionManagerFactory();

                var subscriptionRouter = new SubscriptionRouter(publishers, endpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                var subscriberAddress = context.Receiving.LocalAddress;

                context.Pipeline.Register(b => 
                    new HybridSubscribeTerminator(subscriptionManager, subscriptionRouter, b.Build<IDispatchMessages>(), subscriberAddress, context.Settings.EndpointName()), "Requests the transport to subscribe to a given message type");
                context.Pipeline.Register(b => 
                    new HybridUnsubscribeTerminator(subscriptionManager,subscriptionRouter, b.Build<IDispatchMessages>(), subscriberAddress, context.Settings.EndpointName()), "Sends requests to unsubscribe when message driven subscriptions is in use");

                var authorizer = context.Settings.GetSubscriptionAuthorizer();
                if (authorizer == null)
                {
                    authorizer = _ => true;
                }
                context.Container.RegisterSingleton(authorizer);
                context.Pipeline.Register<SubscriptionReceiverBehavior.Registration>();
            }
        }

        class HybridRouterConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
        {
            readonly DistributionPolicy distributionPolicy;
            readonly IUnicastPublishRouter unicastPublishRouter;

            public HybridRouterConnector(DistributionPolicy distributionPolicy, IUnicastPublishRouter unicastPublishRouter)
            {
                this.distributionPolicy = distributionPolicy;
                this.unicastPublishRouter = unicastPublishRouter;
            }

            public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
            {
                context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

                var eventType = context.Message.MessageType;
                var addressLabels = await GetRoutingStrategies(context, eventType).ConfigureAwait(false);
                //if (addressLabels.Count == 0)
                //{
                //    //No subscribers for this message.
                //    return;
                //}

                var routingStrategies = new List<RoutingStrategy>()
                {
                    new MulticastRoutingStrategy(context.Message.MessageType)
                };

                routingStrategies.AddRange(addressLabels);

                var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(
                    context.Message,
                    routingStrategies,
                    context);

                try
                {
                    await stage(logicalMessageContext).ConfigureAwait(false);
                }
                catch (QueueNotFoundException ex)
                {
                    throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({context.Message.MessageType}) in the routing section of the transport configuration. It may also be the case that the given queue hasn\'t been created yet, or has been deleted.", ex);
                }
            }

            async Task<List<UnicastRoutingStrategy>> GetRoutingStrategies(IOutgoingPublishContext context, Type eventType)
            {
                var addressLabels = await unicastPublishRouter.Route(eventType, distributionPolicy, context).ConfigureAwait(false);
                return addressLabels.ToList();
            }
        }

        class HybridSubscribeTerminator : PipelineTerminator<ISubscribeContext>
        {
            static ILog Logger = LogManager.GetLogger<HybridSubscribeTerminator>();

            readonly SubscriptionRouter subscriptionRouter;

            public HybridSubscribeTerminator(IManageSubscriptions subscriptionManager, SubscriptionRouter subscriptionRouter, IDispatchMessages dispatcher, string subscriberAddress, string subscriberEndpoint)
            {
                this.subscriptionManager = subscriptionManager;
                this.subscriptionRouter = subscriptionRouter;
                this.dispatcher = dispatcher;
                this.subscriberAddress = subscriberAddress;
                this.subscriberEndpoint = subscriberEndpoint;
            }

            protected override async Task Terminate(ISubscribeContext context)
            {
                var eventType = context.EventType;

                await subscriptionManager.Subscribe(eventType, context.Extensions).ConfigureAwait(false);

                var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType);
                //if (publisherAddresses.Count == 0)
                //{
                //    throw new Exception($"No publisher address could be found for message type '{eventType}'. Ensure that a publisher has been configured for the event type and that the configured publisher endpoint has at least one known instance.");
                //}

                var subscribeTasks = new List<Task>(publisherAddresses.Count);
                foreach (var publisherAddress in publisherAddresses)
                {
                    Logger.Debug($"Subscribing to {eventType.AssemblyQualifiedName} at publisher queue {publisherAddress}");

                    var subscriptionMessage = ControlMessageFactory.Create(MessageIntentEnum.Subscribe);

                    subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                    subscriptionMessage.Headers[Headers.ReplyToAddress] = subscriberAddress;
                    subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = subscriberAddress;
                    subscriptionMessage.Headers[Headers.SubscriberEndpoint] = subscriberEndpoint;
                    subscriptionMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
                    subscriptionMessage.Headers[Headers.NServiceBusVersion] = GitVersionInformation.MajorMinorPatch;

                    subscribeTasks.Add(SendSubscribeMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName, context.Extensions));
                }

                await Task.WhenAll(subscribeTasks).ConfigureAwait(false);
            }

            readonly string subscriberAddress;
            readonly string subscriberEndpoint;
            readonly IDispatchMessages dispatcher;

            async Task SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, ContextBag context, int retriesCount = 0)
            {
                var state = context.GetOrCreate<MessageDrivenSubscribeTerminator.Settings>();
                try
                {
                    var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(destination));
                    var transportTransaction = context.GetOrCreate<TransportTransaction>();
                    await dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, context).ConfigureAwait(false);
                }
                catch (QueueNotFoundException ex)
                {
                    if (retriesCount < state.MaxRetries)
                    {
                        await Task.Delay(state.RetryDelay).ConfigureAwait(false);
                        await SendSubscribeMessageWithRetries(destination, subscriptionMessage, messageType, context, ++retriesCount).ConfigureAwait(false);
                    }
                    else
                    {
                        var message = $"Failed to subscribe to {messageType} at publisher queue {destination}, reason {ex.Message}";
                        Logger.Error(message, ex);
                        throw new QueueNotFoundException(destination, message, ex);
                    }
                }
            }

            readonly IManageSubscriptions subscriptionManager;
        }

        class HybridUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
        {
            static ILog Logger = LogManager.GetLogger<HybridUnsubscribeTerminator>();

            protected override async Task Terminate(IUnsubscribeContext context)
            {
                var eventType = context.EventType;

                await subscriptionManager.Unsubscribe(eventType, context.Extensions).ConfigureAwait(false);


                var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType);
                //if (publisherAddresses.Count == 0)
                //{
                //    throw new Exception($"No publisher address could be found for message type {eventType}. Ensure the configured publisher endpoint has at least one known instance.");
                //}

                var unsubscribeTasks = new List<Task>(publisherAddresses.Count);
                foreach (var publisherAddress in publisherAddresses)
                {
                    Logger.Debug("Unsubscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

                    var unsubscribeMessage = ControlMessageFactory.Create(MessageIntentEnum.Unsubscribe);

                    unsubscribeMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                    unsubscribeMessage.Headers[Headers.ReplyToAddress] = replyToAddress;
                    unsubscribeMessage.Headers[Headers.SubscriberTransportAddress] = replyToAddress;
                    unsubscribeMessage.Headers[Headers.SubscriberEndpoint] = endpoint;
                    unsubscribeMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
                    unsubscribeMessage.Headers[Headers.NServiceBusVersion] = GitVersionInformation.MajorMinorPatch;

                    unsubscribeTasks.Add(SendUnsubscribeMessageWithRetries(publisherAddress, unsubscribeMessage, eventType.AssemblyQualifiedName, context.Extensions));
                }
                await Task.WhenAll(unsubscribeTasks).ConfigureAwait(false);
            }

            readonly IManageSubscriptions subscriptionManager;

            public HybridUnsubscribeTerminator(IManageSubscriptions subscriptionManager, SubscriptionRouter subscriptionRouter, IDispatchMessages dispatcher, string replyToAddress, string endpoint)
            {
                this.subscriptionManager = subscriptionManager;
                this.subscriptionRouter = subscriptionRouter;
                this.dispatcher = dispatcher;
                this.replyToAddress = replyToAddress;
                this.endpoint = endpoint;
            }

            async Task SendUnsubscribeMessageWithRetries(string destination, OutgoingMessage unsubscribeMessage, string messageType, ContextBag context, int retriesCount = 0)
            {
                var state = context.GetOrCreate<MessageDrivenUnsubscribeTerminator.Settings>();
                try
                {
                    var transportOperation = new TransportOperation(unsubscribeMessage, new UnicastAddressTag(destination));
                    var transportTransaction = context.GetOrCreate<TransportTransaction>();
                    await dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, context).ConfigureAwait(false);
                }
                catch (QueueNotFoundException ex)
                {
                    if (retriesCount < state.MaxRetries)
                    {
                        await Task.Delay(state.RetryDelay).ConfigureAwait(false);
                        await SendUnsubscribeMessageWithRetries(destination, unsubscribeMessage, messageType, context, ++retriesCount).ConfigureAwait(false);
                    }
                    else
                    {
                        var message = $"Failed to unsubscribe for {messageType} at publisher queue {destination}, reason {ex.Message}";
                        Logger.Error(message, ex);
                        throw new QueueNotFoundException(destination, message, ex);
                    }
                }
            }

            readonly string endpoint;
            readonly IDispatchMessages dispatcher;
            readonly string replyToAddress;
            readonly SubscriptionRouter subscriptionRouter;
        }
    }
}