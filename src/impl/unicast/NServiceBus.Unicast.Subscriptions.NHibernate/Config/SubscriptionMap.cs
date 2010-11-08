using FluentNHibernate.Mapping;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Config
{
    public class SubscriptionMap : ClassMap<Subscription>
    {
        public SubscriptionMap()
        {
            CompositeId()
                .KeyProperty(x => x.SubscriberEndpoint)
                .KeyProperty(x => x.MessageType);

        }
    }
}