using System.IO;
using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.ByteCode.LinFu;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    public class InMemoryDBFixture
    {
        protected ISessionSource sessionSource;
        protected ISubscriptionStorage storage;

        [SetUp]
        public void SetupContext()
        {
            var cfg = SQLiteConfiguration.Standard
                  .UsingFile(Path.GetTempFileName())
                  .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName);

            var fc = Fluently.Configure()
                .Database(cfg)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Subscription>());


            sessionSource = new SessionSource(fc);

            sessionSource.BuildSchema();
            storage = new SubscriptionStorage(sessionSource);
        }
    }
}