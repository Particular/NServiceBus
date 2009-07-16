using System;
using System.IO;
using Common.Logging;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Id;
using NHibernate.Impl;
using NUnit.Framework;
using NBehave.Spec.NUnit;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_configuring_the_saga_persister
    {

        [Test,ExpectedException(typeof(ConfigurationException))]
        public void Exception_should_be_thrown_if_no_sagas_is_found_in_scanned_assemblies()
        {
            var assemblyWithNoSagas = typeof (IBus).Assembly;

            Configure.With(assemblyWithNoSagas)
                .SpringBuilder()
                .NHibernateSagaPersister(SQLiteConfiguration.Standard
                , true);
        }

        [Test]
        public void Sagas_should_automatically_be_mapped_using_conventions()
        {
            var sagaAssembly = typeof(TestSaga).Assembly;

            var config = Configure.With(sagaAssembly)
                .SpringBuilder()
                .NHibernateSagaPersister(SQLiteConfiguration.Standard.InMemory()
                , false);

            var sessionFactory = config.Builder.Build<ISessionFactory>() as SessionFactoryImpl;;

            var persisterForTestSaga = sessionFactory.GetEntityPersister(typeof (TestSaga).FullName);

            persisterForTestSaga.ShouldNotBeNull();

            persisterForTestSaga.IdentifierGenerator.ShouldBeInstanceOfType(typeof(Assigned));

        }
        [Test]
        public void References_of_the_persistent_entity_should_also_be_mapped()
        {
            var sagaAssembly = typeof(TestSaga).Assembly;

            var config = Configure.With(sagaAssembly)
                .SpringBuilder()
                .NHibernateSagaPersister(SQLiteConfiguration.Standard.InMemory()
                , false);

            var sessionFactory = config.Builder.Build<ISessionFactory>() as SessionFactoryImpl; ;

            sessionFactory.GetEntityPersister(typeof(RelatedClass).FullName)
                .ShouldNotBeNull();
        }

        [Test]
        public void Proxy_factory_should_default_to_linfu_if_not_set_by_user()
        {
            var persistenceConfigWithoutProxySpecfied = SQLiteConfiguration.Standard.InMemory();

            var config = Configure.With()
                .SpringBuilder()
                .NHibernateSagaPersister(persistenceConfigWithoutProxySpecfied
                , false);

            config.Builder.Build<ISessionFactory>();

        }

        [Test]
        public void Database_schema_should_be_updated_if_requested()
        {
            bool updateSchema = true;

            var config = Configure.With()
               .SpringBuilder()
               .NHibernateSagaPersister(SQLiteConfiguration.Standard.UsingFile(Path.GetTempFileName())
               , updateSchema);

           var sessionFactory =  config.Builder.Build<ISessionFactory>();

           using (var session = sessionFactory.OpenSession())
            {
                session.CreateCriteria(typeof(TestSaga)).List<TestSaga>();
            }
        }
    }
}
