using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Connection;
using NHibernate.Dialect;
using NHibernate.Impl;
using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;
using NBehave.Spec.NUnit;
using Rhino.Mocks;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_configuring_the_saga_persister
    {
        private Configure config;

        [SetUp]
        public void SetUp()
        {
            var properties = SQLiteConfiguration.Standard.UsingFile(".\\NServiceBus.Sagas.sqlite").ToProperties();
         
            config = Configure.With()
                .SpringBuilder()
                .Sagas()
                .NHibernateSagaPersister();
        }

        [Test]
        public void Persister_should_be_registered_as_single_call()
        {
            var persister = config.Builder.Build<SagaPersister>();

            persister.ShouldNotBeTheSameAs(config.Builder.Build<SagaPersister>());
        }


        [Test]
        public void Message_module_for_session_management_should_be_registered_singleton()
        {
            var module = config.Builder.Build<NHibernateMessageModule>();

            module.ShouldNotBeNull();
            module.ShouldBeTheSameAs(config.Builder.Build<NHibernateMessageModule>());
        }

        [Test]
        public void The_sessionfactory_should_be_built_and_registered_as_singleton()
        {
            var sessionFactory = config.Builder.Build<ISessionFactory>();

            sessionFactory.ShouldNotBeNull();
            sessionFactory.ShouldBeTheSameAs(config.Builder.Build<ISessionFactory>());

        }

        [Test]
        public void Database_settings_should_be_read_from_custom_config_section()
        {
           
            var sessionFactory = config.Builder.Build<ISessionFactory>() as SessionFactoryImpl;

            sessionFactory.Dialect.ShouldBeInstanceOfType(typeof(SQLiteDialect));

         
            sessionFactory.ConnectionProvider.GetConnection().ConnectionString.ShouldEqual("Data Source=.\\DBFileNameFromAppConfig.sqlite;Version=3;New=True;");
        }


        [Test]
        public void Persister_should_default_to_sqlite_if_config_section_is_missing()
        {
            var configSource = MockRepository.GenerateStub<IConfigurationSource>();

            var configWithMissingSection = Configure.With()
               .SpringBuilder()
               .CustomConfigurationSource(configSource)
               .Sagas()
               .NHibernateSagaPersister();

            var sessionFactory = configWithMissingSection.Builder.Build<ISessionFactory>() as SessionFactoryImpl;

            sessionFactory.ConnectionProvider.GetConnection().ConnectionString.ShouldEqual("Data Source=.\\NServiceBus.Sagas.sqlite;Version=3;New=True;");

        }

        [Test]
        public void Update_schema_can_be_specified_by_the_user()
        {

        }

    }
}
