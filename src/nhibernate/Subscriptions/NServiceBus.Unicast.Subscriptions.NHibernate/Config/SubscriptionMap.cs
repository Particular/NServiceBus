using System;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Config
{
  public class SubscriptionMap : ClassMapping<Subscription>
  {
    public SubscriptionMap()
    {
      ComposedId(x =>
                   {
                     // Maximum keylength for sql server is 900 bytes
                     Action<IColumnMapper> columnMapper = col =>
                                             {
                                               col.Length(450);
                                               col.SqlType("VARCHAR(450)");
                                             };

                     x.Property(p => p.SubscriberEndpoint, map => map.Column(columnMapper));
                     x.Property(p => p.MessageType, map => map.Column(columnMapper));
                   });
    }
  }
}