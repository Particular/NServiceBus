namespace NServiceBus.TimeoutPersisters.NHibernate.Config
{
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;

    public class TimeoutEntityMap : ClassMapping<TimeoutEntity>
    {
        public TimeoutEntityMap()
        {
            Id(x => x.Id, m => m.Generator(Generators.Assigned));
            Property(p => p.State);
            Property(p => p.CorrelationId, pm => pm.Length(1024));
            Property(p => p.Destination, pm =>
                                             {
                                                 pm.Type<AddressUserType>();
                                                 pm.Length(1024);
                                             });
            Property(p => p.SagaId, pm => pm.Index("SagaIdIdx"));
            Property(p => p.Time);
            Property(p => p.Headers, pm => pm.Type(NHibernateUtil.StringClob));
            Property(p => p.Endpoint, pm =>
                                          {
                                              pm.Index("EndpointIdx");
                                              pm.Length(1024);
                                          });
        }
    }
}
