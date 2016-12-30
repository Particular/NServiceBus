namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ObjectBuilder;
    using Routing;
    using Settings;
    using Transport;

    class RoutingComponent : IRoutingComponent
    {
        public UnicastRoutingTable Sending => settings.Get<UnicastRoutingTable>();
        public UnicastSubscriberTable Publishing => settings.Get<UnicastSubscriberTable>();
        public EndpointInstances EndpointInstances => settings.Get<EndpointInstances>();

        public RoutingComponent(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        public void RegisterSubscriptionHandler(Func<IBuilder, IManageSubscriptions> handlerFactory)
        {
            Guard.AgainstNull(nameof(handlerFactory), handlerFactory);
            subscriptionHandlers.Add(handlerFactory);
        }

        public IEnumerable<IManageSubscriptions> BuildSubscriptionHandlers(IBuilder builder)
        {
            return subscriptionHandlers.Select(x => x(builder));
        }

        ReadOnlySettings settings;
        List<Func<IBuilder, IManageSubscriptions>> subscriptionHandlers = new List<Func<IBuilder, IManageSubscriptions>>();
    }
}