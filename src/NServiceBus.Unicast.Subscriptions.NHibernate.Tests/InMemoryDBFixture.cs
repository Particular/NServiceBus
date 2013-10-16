namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using System.IO;
    using global::NHibernate.Cfg;
    using global::NHibernate.Dialect;
    using global::NHibernate.Mapping.ByCode;
    using global::NHibernate.Tool.hbm2ddl;
    using MessageDrivenSubscriptions;
    using NUnit.Framework;

    public class InMemoryDBFixture
    {
        protected ISubscriptionStorage storage;
        protected ISubscriptionStorageSessionProvider subscriptionStorageSessionProvider;

        [SetUp]
        public void SetupContext()
        {
          var cfg = new Configuration()
            .DataBaseIntegration(x =>
            {
              x.Dialect<SQLiteDialect>();
              x.ConnectionString = string.Format(@"Data Source={0};Version=3;New=True;", Path.GetTempFileName());
            });

          var mapper = new ModelMapper();
          mapper.AddMappings(typeof(NHibernate.Config.SubscriptionMap).Assembly.GetExportedTypes());
          var faultMappings = mapper.CompileMappingForAllExplicitlyAddedEntities();

          cfg.AddMapping(faultMappings);

          new SchemaExport(cfg).Create(false, true);

           subscriptionStorageSessionProvider = new SubscriptionStorageSessionProvider(cfg.BuildSessionFactory());

           storage = new SubscriptionStorage(subscriptionStorageSessionProvider);
        }
    }
}