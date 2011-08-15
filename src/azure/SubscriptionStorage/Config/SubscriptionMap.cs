using FluentNHibernate.Mapping;

namespace NServiceBus.Unicast.Subscriptions.Azure.TableStorage.Config
{
    public sealed class SubscriptionMap : ClassMap<Subscription>
    {
        public SubscriptionMap()
        {
            CompositeId()
                .KeyProperty(x => x.SubscriberEndpoint, "RowKey")
                .KeyProperty(x => x.MessageType, "PartitionKey");
        }
    }
}