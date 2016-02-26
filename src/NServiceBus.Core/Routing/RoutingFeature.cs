namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Config;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class RoutingFeature : Feature
    {
        public RoutingFeature()
        {
            EnableByDefault();
            Defaults(s =>
            {
                s.SetDefault<UnicastRoutingTable>(new UnicastRoutingTable());
                s.SetDefault<EndpointInstances>(new EndpointInstances());
                s.SetDefault<Publishers>(new Publishers());
                s.SetDefault<DistributionPolicy>(new DistributionPolicy());
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();

            context.Container.ConfigureComponent(b => context.Settings.Get<UnicastRoutingTable>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => context.Settings.Get<EndpointInstances>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => context.Settings.Get<Publishers>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => context.Settings.Get<DistributionPolicy>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<UnicastRouter>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new UnicastSendRouterConnector(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), b.Build<UnicastRouter>(), b.Build<DistributionPolicy>()), DependencyLifecycle.InstancePerCall);
            
            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            if (unicastBusConfig != null)
            {
                var routeTable = context.Settings.Get<UnicastRoutingTable>();
                var publishers = context.Settings.Get<Publishers>();
                var legacyRoutingConfig = unicastBusConfig.MessageEndpointMappings;
                var conventions = context.Settings.Get<Conventions>();

                var knownMessageTypes = context.Settings.GetAvailableTypes()
                    .Where(conventions.IsMessageType)
                    .ToList();

                foreach (MessageEndpointMapping m in legacyRoutingConfig)
                {
                    m.Configure((type, s) => routeTable.RouteToAddress(type, transportInfrastructure.MakeCanonicalForm(s)));
                    m.Configure((type, s) =>
                    {
                        var typesEnclosed = knownMessageTypes.Where(t => t.IsAssignableFrom(type));
                        foreach (var t in typesEnclosed)
                        {
                            publishers.AddStatic(s, t);
                        }
                    });
                }
            }

            context.RegisterStartupTask(b => new SubscriptionStoreRouteInformationProvider(context.Settings, b));
            var outboundRoutingPolicy = transportInfrastructure.OutboundRoutingPolicy;
            context.Pipeline.Register("UnicastSendRouterConnector", typeof(UnicastSendRouterConnector), "Determines how the message being sent should be routed");
            context.Pipeline.Register("UnicastReplyRouterConnector", typeof(UnicastReplyRouterConnector), "Determines how replies should be routed");
            if (outboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
            {
                context.Pipeline.Register("UnicastPublishRouterConnector", typeof(UnicastPublishRouterConnector), "Determines how the published messages should be routed");
            }
            else
            {
                context.Pipeline.Register("MulticastPublishRouterBehavior", typeof(MulticastPublishRouterBehavior), "Determines how the published messages should be routed");                
            }

            if (canReceive)
            {
                var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");
                var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
                context.Pipeline.Register("ApplyReplyToAddress", new ApplyReplyToAddressBehavior(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), publicReturnAddress, distributorAddress), "Applies the public reply to address to outgoing messages");

                if (outboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
                {
                    context.Container.ConfigureComponent<SubscriptionRouter>(DependencyLifecycle.SingleInstance);

                    context.Pipeline.Register("MessageDrivenSubscribeTerminator", typeof(MessageDrivenSubscribeTerminator), "Sends subscription requests when message driven subscriptions is in use");
                    context.Pipeline.Register("MessageDrivenUnsubscribeTerminator", typeof(MessageDrivenUnsubscribeTerminator), "Sends requests to unsubscribe when message driven subscriptions is in use");

                    var subscriberAddress = distributorAddress ?? context.Settings.LocalAddress();
                    context.Container.ConfigureComponent(b => new MessageDrivenSubscribeTerminator(b.Build<SubscriptionRouter>(), subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), DependencyLifecycle.SingleInstance);
                    context.Container.ConfigureComponent(b => new MessageDrivenUnsubscribeTerminator(b.Build<SubscriptionRouter>(), subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), DependencyLifecycle.SingleInstance);
                }
                else
                {
                    var transportSubscriptionInfrastructure = transportInfrastructure.ConfigureSubscriptionInfrastructure();
                    var subscriptionManager = transportSubscriptionInfrastructure.SubscriptionManagerFactory();

                    context.Container.RegisterSingleton(subscriptionManager);
                    context.Pipeline.Register("NativeSubscribeTerminator", typeof(NativeSubscribeTerminator), "Requests the transport to subscribe to a given message type");
                    context.Pipeline.Register("NativeUnsubscribeTerminator", typeof(NativeUnsubscribeTerminator), "Requests the transport to unsubscribe to a given message type");
                }
            }
        }

        class SubscriptionStoreRouteInformationProvider : FeatureStartupTask
        {
            ReadOnlySettings settings;
            IBuilder builder;

            public SubscriptionStoreRouteInformationProvider(ReadOnlySettings settings, IBuilder builder)
            {
                this.settings = settings;
                this.builder = builder;
            }

            protected override Task OnStart(IMessageSession session)
            {
                var transportInfrastructure = settings.Get<TransportInfrastructure>();
                if (transportInfrastructure.OutboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast) //Publish via send
                {
                    var subscriptions = builder.BuildAll<ISubscriptionStorage>().FirstOrDefault();
                    if (subscriptions != null)
                    {
                        settings.Get<UnicastRoutingTable>().AddDynamic((t, c) => QuerySubscriptionStore(subscriptions, t, c));
                    }
                }
                return TaskEx.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }

            static async Task<IEnumerable<IUnicastRoute>> QuerySubscriptionStore(ISubscriptionStorage subscriptions, List<Type> types, ContextBag contextBag)
            {
                if (!(contextBag is IOutgoingPublishContext))
                {
                    return new List<IUnicastRoute>();
                }

                var messageTypes = types.Select(t => new MessageType(t));
                var subscribers = await subscriptions.GetSubscriberAddressesForMessage(messageTypes, contextBag).ConfigureAwait(false);
                return subscribers.Select(s => new SubscriberDestination(s));
            }

            class SubscriberDestination : IUnicastRoute
            {
                UnicastRoutingTarget target;

                public SubscriberDestination(Subscriber subscriber)
                {
                    if (subscriber.Endpoint != null)
                    {
                        target = UnicastRoutingTarget.ToAnonymousInstance(subscriber.Endpoint, subscriber.TransportAddress);
                    }
                    else
                    {
                        target = UnicastRoutingTarget.ToTransportAddress(subscriber.TransportAddress);
                    }
                }

                public Task<IEnumerable<UnicastRoutingTarget>> Resolve(Func<EndpointName, Task<IEnumerable<EndpointInstance>>> instanceResolver)
                {
                    return Task.FromResult(EnumerableEx.Single(target));
                }
            }
        }
    }
}