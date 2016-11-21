namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Pipeline;
    using Routing;
    using Transport;

    class NativePublishSubscribeProvider : IPublishSubscribeProvider
    {
        TransportSubscriptionInfrastructure transportSubscriptionInfrastructure;

        public NativePublishSubscribeProvider(TransportSubscriptionInfrastructure transportSubscriptionInfrastructure)
        {
            this.transportSubscriptionInfrastructure = transportSubscriptionInfrastructure;
        }

        public Func<IBuilder, IManageSubscriptions> GetSubscriptionManager(FeatureConfigurationContext context)
        {
            return _ => transportSubscriptionInfrastructure.SubscriptionManagerFactory();
        }

        Func<IBuilder, IPublishRouter> IPublishSubscribeProvider.GetRouter(FeatureConfigurationContext context)
        {
            return _ => new MulticastPublishRouter();
        }

        class MulticastPublishRouter : IPublishRouter
        {
            public Task<RoutingStrategy[]> GetRoutingStrategies(IOutgoingPublishContext context)
            {
                return Task.FromResult(new RoutingStrategy[]
                {
                new MulticastRoutingStrategy(context.Message.MessageType)
                });
            }
        }
    }
}