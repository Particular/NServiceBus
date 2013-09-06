using System.IO;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    using MessageDrivenSubscriptions;

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