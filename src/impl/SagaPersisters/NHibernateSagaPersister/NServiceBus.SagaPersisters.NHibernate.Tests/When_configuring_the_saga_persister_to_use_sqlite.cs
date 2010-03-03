using System;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Connection;
using NHibernate.Dialect;
using NHibernate.Impl;
using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;
using NBehave.Spec.NUnit;
using Rhino.Mocks;
using NServiceBus.Saga;

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

            persister.ShouldNotBeTheSameAs(config.Builder.Build<SagaPersister>());
        }

        [Test]
        public void The_sessionfactory_should_be_built_and_registered_as_singleton()
        {
            var sessionFactory = config.Builder.Build<ISessionFactory>();

            sessionFactory.ShouldNotBeNull();
            sessionFactory.ShouldBeTheSameAs(config.Builder.Build<ISessionFactory>());

        }



        
    }

  
}
