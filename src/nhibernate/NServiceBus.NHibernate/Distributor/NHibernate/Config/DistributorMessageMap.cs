namespace NServiceBus.Distributor.NHibernate.Config
{
    using Persistence.NHibernate;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Mapping.ByCode.Conformist;

    /// <summary>
    /// Distributor message map class
    /// </summary>
    public class DistributorMessageMap : ClassMapping<DistributorMessage>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public DistributorMessageMap()
        {
            Table("DistributorMessages");

            Id(x => x.Id, m => m.Generator(Generators.Identity));
            Property(p => p.Destination, pm =>
                                             {
                                                 pm.Type<AddressUserType>();
                                                 pm.Length(1024);
                                             });
            Property(p => p.Endpoint, pm =>
                                          {
                                              pm.Index("EndpointIdx");
                                              pm.Length(1024);
                                          });
        }
    }
}