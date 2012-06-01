using System.IO;
using NHibernate;
using NServiceBus.SagaPersisters.NHibernate.Config.Internal;
using NServiceBus.UnitOfWork;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using UnitOfWork.NHibernate;

    public class InMemoryFixture
    {
        protected IManageUnitsOfWork UnitOfWork;
        protected SagaPersister SagaPersister;
        protected ISessionFactory SessionFactory;

        [SetUp]
        public void SetUp()
        {
            var nhibernateProperties = SQLiteConfiguration.UsingFile(Path.GetTempFileName());

            SessionFactory = new SessionFactoryBuilder(typeof(TestSaga).Assembly.GetTypes())
                .Build(nhibernateProperties, true);

            SagaPersister = new SagaPersister { SessionFactory = SessionFactory };

            UnitOfWork = new UnitOfWorkManager { SessionFactory = SessionFactory };
        }
    }
}