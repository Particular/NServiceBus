using System.Linq;
using NHibernate;
using NServiceBus.UnitOfWork.NHibernate;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_configuring_the_saga_persister_from_appconfig
    {
        private Configure config;

        [SetUp]
        public void SetUp()
        {

            config = Configure.With(new[] { typeof(MySaga).Assembly })
                .DefaultBuilder()
                .Sagas()
                .NHibernateSagaPersister();
        }

        [Test]
        public void Update_schema_can_be_specified_by_the_user()
        {
            var sessionFactory = config.Builder.Build<ISessionFactory>();

            using (var session = sessionFactory.OpenSession())
            {
                session.CreateCriteria(typeof(MySaga)).List<MySaga>();
            }
        }

        [Test]
        public void UnitOfWork_Should_be_configured()
        {
            var uow = config.Builder.Build<UnitOfWorkManager>();

            Assert.IsNotNull(uow);
        }

        [Test]
        public void Handles_Multiple_registrations_of_UnitOfWork()
        {
            config.NHibernateUnitOfWork();

            var uow = config.Builder.BuildAll<UnitOfWorkManager>().ToList();


            Assert.IsNotNull(uow);
            Assert.That(uow, Has.Count.EqualTo(1));
        }
    }
}