using System.IO;
using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.ByteCode.LinFu;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    public class InMemoryDBFixture
    {
        protected ISubscriptionStorage storage;
        protected ISubscriptionStorageSessionProvider subscriptionStorageSessionProvider;

        [SetUp]
        public void SetupContext()
        {
            var cfg = SQLiteConfiguration.Standard
                  .UsingFile(Path.GetTempFileName())
                  .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName);

            var fc = Fluently.Configure()
                .Database(cfg)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Subscription>())
                .ExposeConfiguration(config => new SchemaExport(config).Create(true, true));

           subscriptionStorageSessionProvider = new SubscriptionStorageSessionProvider(fc.BuildSessionFactory());

           storage = new SubscriptionStorage(subscriptionStorageSessionProvider);
        }
    }
}