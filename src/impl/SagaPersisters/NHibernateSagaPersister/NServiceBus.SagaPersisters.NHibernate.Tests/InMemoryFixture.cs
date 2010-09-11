using System.IO;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.ByteCode.LinFu;
using NServiceBus.SagaPersisters.NHibernate.Config.Internal;
using NServiceBus.UnitOfWork;
using NUnit.Framework;
using NServiceBus.UnitOfWork.NHibernate;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    public class InMemoryFixture
    {
        protected IManageUnitsOfWork UnitOfWork;
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

            SagaPersister = new SagaPersister { SessionFactory = SessionFactory };

            UnitOfWork = new UnitOfWorkManager { SessionFactory = SessionFactory };
        }
    }
}