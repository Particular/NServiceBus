using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Testing;
using NHibernate;
using NH = NHibernate;
using NHibernate.Context;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests
{
    public class InMemoryDBFixture
    {
        protected ISession session;
        protected ISessionSource sessionSource;
        protected ISessionFactory sessionFactory;
        
        [SetUp]
        public void SetupContext()
        {
            Before_each_test();
        }

      
        protected virtual void Before_each_test()
        {

            var cfg = SQLiteConfiguration.Standard
                .InMemory()
                .ShowSql()
                .Raw("proxyfactory.factory_class",
                     "NHibernate.ByteCode.LinFu.ProxyFactoryFactory, NHibernate.ByteCode.LinFu");

            var fc = Fluently.Configure()
                .Database(cfg)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Subscription>());

          
            sessionSource = new SingleConnectionSessionSourceForSQLiteInMemoryTesting(fc);
           
            sessionSource.BuildSchema();
            session = sessionSource.CreateSession();
        }

    }
}