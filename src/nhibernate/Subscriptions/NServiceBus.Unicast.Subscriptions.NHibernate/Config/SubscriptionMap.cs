using NHibernate.Mapping.ByCode.Conformist;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Config
{
  public class SubscriptionMap : ClassMapping<Subscription>
  {
    public SubscriptionMap()
    {
      ComposedId(x =>
                   {
                     x.Property(p => p.SubscriberEndpoint);
                     x.Property(p => p.MessageType);
                   });
    }
  }

    //public class SubscriptionMap : ClassMap<Subscription>
    //{
    //    public SubscriptionMap()
    //    {
    //        CompositeId()
    //            .KeyProperty(x => x.SubscriberEndpoint)
    //            .KeyProperty(x => x.MessageType);

    //    }
    //}
}