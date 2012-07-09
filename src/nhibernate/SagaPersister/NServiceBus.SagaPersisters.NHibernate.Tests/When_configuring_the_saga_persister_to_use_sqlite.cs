using NHibernate;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_configuring_the_saga_persister_to_use_sqlite
    {
        private Configure config;

        [SetUp]
        public void SetUp()
        {
            config = Configure.With(new[] { typeof(MySaga).Assembly})
                .DefaultBuilder()
                .Sagas()
                .NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration();
        }

        [Test]
        public void Persister_should_be_registered_as_single_call()
        {
            var persister = config.Builder.Build<SagaPersister>();

            Assert.AreNotEqual(persister,config.Builder.Build<SagaPersister>());
        }

        [Test]
        public void The_sessionfactory_should_be_built_and_registered_as_singleton()
        {
            var sessionFactory = config.Builder.Build<ISessionFactory>();

            Assert.NotNull(sessionFactory);
            Assert.AreEqual(sessionFactory,config.Builder.Build<ISessionFactory>());
        }
    }
}
