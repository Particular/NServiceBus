namespace NServiceBus.Deduplication.NHibernate.Config
{
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;

    /// <summary>
    /// Deduplication message mapping class.
    /// </summary>
    public class DeduplicationMessageMap : ClassMapping<DeduplicationMessage>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public DeduplicationMessageMap()
        {
            Table("GatewayDeduplication");
            Id(x => x.Id, m => m.Generator(Generators.Assigned));
            Property(p => p.TimeReceived);
        }
    }
}