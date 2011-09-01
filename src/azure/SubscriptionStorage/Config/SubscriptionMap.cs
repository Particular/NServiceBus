using NHibernate.Mapping.ByCode.Conformist;

namespace NServiceBus.Unicast.Subscriptions.Azure.TableStorage.Config
{
    public sealed class SubscriptionMap : ClassMapping<Subscription>
    {
        public SubscriptionMap()
        {
          ComposedId(id =>
                       {
                         id.Property(x => x.SubscriberEndpoint, m => m.Column("RowKey"));
                         id.Property(x => x.MessageType, m => m.Column("PartitionKey"));
                       });
        }
    }
}