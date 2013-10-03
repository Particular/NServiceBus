namespace NServiceBus.TimeoutPersisters.NHibernate.Config
{
    using global::NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;
    using Persistence.NHibernate;

    /// <summary>
    /// Timeout entity map class
    /// </summary>
    public class TimeoutEntityMap : ClassMapping<TimeoutEntity>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public TimeoutEntityMap()
        {
            Id(x => x.Id, m => m.Generator(Generators.Assigned));
            Property(p => p.State, pm => pm.Type(NHibernateUtil.BinaryBlob));
            Property(p => p.CorrelationId, pm => pm.Length(1024));
            Property(p => p.Destination, pm =>
            {
                pm.Type<AddressUserType>();
                pm.Length(1024);
            });
            Property(p => p.SagaId, pm => pm.Index("TimeoutEntity_SagaIdIdx"));
            Property(p => p.Time, pm => pm.Index("TimeoutEntity_EndpointIdx"));
            Property(p => p.Headers, pm => pm.Type(NHibernateUtil.StringClob));
            Property(p => p.Endpoint, pm =>
            {
                pm.Index("TimeoutEntity_EndpointIdx");
                pm.Length(440);
            });
        }
    }
}
