using FluentNHibernate.Mapping;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Config
{
    public class SubscriptionMap : ClassMap<Subscription>
    {
        public SubscriptionMap()
        {
            UseCompositeId()
                .WithKeyProperty(x => x.SubscriberEndpoint)
                .WithKeyProperty(x => x.MessageType);
        }
    }
}