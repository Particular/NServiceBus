using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using FluentNHibernate.Cfg.Db;
using NBehave.Spec.NUnit;
using NHibernate;
using NHibernate.Id;
using NHibernate.Impl;
using NServiceBus.SagaPersisters.NHibernate.Config.Internal;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_building_the_sessionfactory
    {
        private IDictionary<string, string> testProperties = SQLiteConfiguration.Standard
            .InMemory()
            .ProxyFactoryFactory(SessionFactoryBuilder.LINFU_PROXYFACTORY)
            .ToProperties();
       
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void Exception_should_be_thrown_if_no_sagas_is_found_in_scanned_assemblies()
        {
            var assemblyWithNoSagas = typeof(IBus).Assembly;

            new SessionFactoryBuilder(assemblyWithNoSagas.GetTypes())
                .Build(testProperties,false);
        }

        [Test]
        public void Sagas_should_automatically_be_mapped_using_conventions()
        {
            var assemblyContainingSagas = typeof(TestSaga).Assembly;

            var builder = new SessionFactoryBuilder(assemblyContainingSagas.GetTypes());

            var sessionFactory = builder.Build(testProperties,false) as SessionFactoryImpl; ;

            var persisterForTestSaga = sessionFactory.GetEntityPersister(typeof(TestSaga).FullName);

            persisterForTestSaga.ShouldNotBeNull();

            persisterForTestSaga.IdentifierGenerator.ShouldBeInstanceOfType(typeof(Assigned));


            var persisterForOrderLine = sessionFactory.GetEntityPersister(typeof(OrderLine).FullName);
            persisterForOrderLine.IdentifierGenerator.ShouldBeInstanceOfType(typeof(GuidCombGenerator));

        }

        [Test]
        public void Proxy_factory_should_default_to_linfu_if_not_set_by_user()
        {
            var persistenceConfigWithoutProxySpecfied = SQLiteConfiguration.Standard.InMemory();

            var nhibernateProperties = persistenceConfigWithoutProxySpecfied.ToProperties();
            nhibernateProperties.Remove(SessionFactoryBuilder.PROXY_FACTORY_KEY);


            new SessionFactoryBuilder(typeof(TestSaga).Assembly.GetTypes())
                .Build(nhibernateProperties, false);

        }

        [Test]
        public void References_of_the_persistent_entity_should_also_be_mapped()
        {
            var assemblyContainingSagas = typeof(TestSaga).Assembly;

            var builder = new SessionFactoryBuilder(assemblyContainingSagas.GetTypes());

            var sessionFactory = builder.Build(testProperties,false) as SessionFactoryImpl; ;

            sessionFactory.GetEntityPersister(typeof(RelatedClass).FullName)
                .ShouldNotBeNull();
        }

        [Test]
        public void Database_schema_should_be_updated_if_requested()
        {
            var nhibernateProperties = SQLiteConfiguration.Standard
                .UsingFile(Path.GetTempFileName())
                .ProxyFactoryFactory(SessionFactoryBuilder.LINFU_PROXYFACTORY)
                .ToProperties();

            var sessionFactory = new SessionFactoryBuilder(typeof(TestSaga).Assembly.GetTypes())
                .Build(nhibernateProperties, true);

            using (var session = sessionFactory.OpenSession())
            {
                session.CreateCriteria(typeof(TestSaga)).List<TestSaga>();
            }
        }
    }
}