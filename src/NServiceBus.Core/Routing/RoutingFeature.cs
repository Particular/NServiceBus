﻿namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Config;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
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
                s.SetDefault<TransportAddresses>(new TransportAddresses());
            });
            RegisterStartupTask<SubscriptionStoreRouteInformationProvider>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var transportDefinition = context.Settings.Get<TransportDefinition>();

            context.Container.ConfigureComponent(b => context.Settings.Get<UnicastRoutingTable>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => context.Settings.Get<EndpointInstances>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => context.Settings.Get<Publishers>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => context.Settings.Get<DistributionPolicy>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<UnicastRouter>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new UnicastSendRouterConnector(LocalAddress(b), b.Build<UnicastRouter>(), b.Build<DistributionPolicy>()), DependencyLifecycle.InstancePerCall);

            var transportAddresses = context.Settings.Get<TransportAddresses>();
            transportAddresses.RegisterTransportDefault(x => transportDefinition.ToTransportAddress(new LogicalAddress(x)));
            context.Container.ConfigureComponent(b => transportAddresses, DependencyLifecycle.SingleInstance);

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
                    m.Configure(routeTable.AddStatic);
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

            var outboundRoutingPolicy = transportDefinition.GetOutboundRoutingPolicy(context.Settings);
            context.Pipeline.Register("UnicastSendRouterConnector", typeof(UnicastSendRouterConnector), "Determines how the message being sent should be routed");
            context.Pipeline.Register("UnicastReplyRouterConnector", typeof(UnicastReplyRouterConnector), "Determines how replies should be routed");
            if (outboundRoutingPolicy.Publishes == OutboundRoutingType.DirectSend)
            {
                context.Pipeline.Register("UnicastPublishRouterConnector", typeof(UnicastPublishRouterConnector), "Determines how the published messages should be routed");
            }
            else
            {
                context.Pipeline.Register("MulticastPublishRouterBehavior", typeof(MulticastPublishRouterBehavior), "Determines how the published messages should be routed");                
            }

            if (canReceive)
            {
                context.Pipeline.Register("ApplyReplyToAddress", typeof(ApplyReplyToAddressBehavior), "Applies the public reply to address to outgoing messages");

                context.Container.ConfigureComponent(b => new ApplyReplyToAddressBehavior(ReplyToAddress(b)), DependencyLifecycle.SingleInstance);

                if (outboundRoutingPolicy.Publishes == OutboundRoutingType.DirectSend)
                {
                    context.Container.ConfigureComponent<SubscriptionRouter>(DependencyLifecycle.SingleInstance);

                    context.Pipeline.Register("MessageDrivenSubscribeTerminator", typeof(MessageDrivenSubscribeTerminator), "Sends subscription requests when message driven subscriptions is in use");
                    context.Pipeline.Register("MessageDrivenUnsubscribeTerminator", typeof(MessageDrivenUnsubscribeTerminator), "Sends requests to unsubscribe when message driven subscriptions is in use");

                    context.Container.ConfigureComponent(b => new MessageDrivenSubscribeTerminator(b.Build<SubscriptionRouter>(), ReplyToAddress(b), b.Build<IDispatchMessages>()), DependencyLifecycle.SingleInstance);
                    context.Container.ConfigureComponent(b => new MessageDrivenUnsubscribeTerminator(b.Build<SubscriptionRouter>(), ReplyToAddress(b), b.Build<IDispatchMessages>()), DependencyLifecycle.SingleInstance);
                }
                else
                {
                    context.Container.RegisterSingleton(transportDefinition.GetSubscriptionManager());
                    context.Pipeline.Register("NativeSubscribeTerminator", typeof(NativeSubscribeTerminator), "Requests the transport to subscribe to a given message type");
                    context.Pipeline.Register("NativeUnsubscribeTerminator", typeof(NativeUnsubscribeTerminator), "Requests the transport to unsubscribe to a given message type");
                }
            }
        }

        static string ReplyToAddress(IBuilder builder)
        {
            var settings = builder.Build<ReadOnlySettings>();
            string replyToAddress;

            if (!settings.TryGet("PublicReturnAddress", out replyToAddress))
            {
                replyToAddress = settings.LocalAddress();
            }
            return replyToAddress;
        }

        static string LocalAddress(IBuilder builder)
        {
            return builder.Build<ReadOnlySettings>().LocalAddress();
        }

        public class SubscriptionStoreRouteInformationProvider : FeatureStartupTask
        {
            ReadOnlySettings settings;
            IBuilder builder;

            public SubscriptionStoreRouteInformationProvider(ReadOnlySettings settings, IBuilder builder)
            {
                this.settings = settings;
                this.builder = builder;
            }

            protected override Task OnStart(IBusContext context)
            {
                var transportDefinition = settings.Get<TransportDefinition>();
                if (transportDefinition.GetOutboundRoutingPolicy(settings).Publishes == OutboundRoutingType.DirectSend) //Publish via send
                {
                    var subscriptions = builder.BuildAll<ISubscriptionStorage>().FirstOrDefault();
                    if (subscriptions != null)
                    {
                        settings.Get<UnicastRoutingTable>().AddDynamic((t, c) => QuerySubscriptionStore(subscriptions, t, c));
                    }
                }
                return TaskEx.Completed;
            }

            private static IEnumerable<UnicastRoutingDestination> QuerySubscriptionStore(ISubscriptionStorage subscriptions, Type messageType, ContextBag contextBag)
            {
                var messageTypes = new[]
                {
                    new MessageType(messageType)
                };
                return subscriptions.GetSubscriberAddressesForMessage(messageTypes, contextBag).GetAwaiter().GetResult().Select(s => new UnicastRoutingDestination(s));
            }
        }
    }
}