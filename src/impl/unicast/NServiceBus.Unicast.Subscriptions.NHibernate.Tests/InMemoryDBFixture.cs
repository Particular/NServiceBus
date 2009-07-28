using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Testing;
using NHibernate;
using NHibernate.ByteCode.LinFu;
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
                .ProxyFactoryFactory(typeof (ProxyFactoryFactory).AssemblyQualifiedName);

            var fc = Fluently.Configure()
                .Database(cfg)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Subscription>());

          
            sessionSource = new SingleConnectionSessionSourceForSQLiteInMemoryTesting(fc);
           
            sessionSource.BuildSchema();
            session = sessionSource.CreateSession();
        }

    }
}