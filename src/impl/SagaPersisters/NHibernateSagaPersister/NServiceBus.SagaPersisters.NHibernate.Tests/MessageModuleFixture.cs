using System.IO;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.ByteCode.LinFu;
using NServiceBus.SagaPersisters.NHibernate.Config.Internal;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    public class MessageModuleFixture
    {
        protected IMessageModule MessageModule;
        protected SagaPersister SagaPersister;
        protected ISessionFactory SessionFactory;

        [SetUp]
        public void SetUp()
        {
            var nhibernateProperties = SQLiteConfiguration.Standard
                .UsingFile(Path.GetTempFileName())
                .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
                .ToProperties();

            SessionFactory = new SessionFactoryBuilder(typeof(TestSaga).Assembly.GetTypes())
                .Build(nhibernateProperties, true);


            MessageModule = new NHibernateMessageModule { SessionFactory = SessionFactory };
            SagaPersister = new SagaPersister { SessionFactory = SessionFactory };
        }
    }
}