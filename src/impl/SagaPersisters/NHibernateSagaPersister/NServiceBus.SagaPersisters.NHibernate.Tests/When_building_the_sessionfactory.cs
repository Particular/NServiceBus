using System.Collections.Generic;
using System.IO;
using FluentNHibernate.Cfg.Db;
using NHibernate.ByteCode.LinFu;
using NHibernate.Impl;
using NServiceBus.SagaPersisters.NHibernate.Config.Internal;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_building_the_sessionfactory
    {
        private readonly IDictionary<string, string> testProperties = SQLiteConfiguration.Standard
            .InMemory()
            .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
            .ToProperties();

       
        [Test]
        public void Proxy_factory_should_default_to_linfu_if_not_set_by_user()
        {
            var persistenceConfigWithoutProxySpecfied = SQLiteConfiguration.Standard.InMemory();

            var nhibernateProperties = persistenceConfigWithoutProxySpecfied.ToProperties();
            nhibernateProperties.Remove("proxyfactory.factory_class");


            new SessionFactoryBuilder(typeof(TestSaga).Assembly.GetTypes())
                .Build(nhibernateProperties, false);

        }

        [Test]
        public void References_of_the_persistent_entity_should_also_be_mapped()
        {
            var assemblyContainingSagas = typeof(TestSaga).Assembly;

            var builder = new SessionFactoryBuilder(assemblyContainingSagas.GetTypes());

            var sessionFactory = builder.Build(testProperties, false) as SessionFactoryImpl;

            Assert.NotNull(sessionFactory.GetEntityPersister(typeof(RelatedClass).FullName));
        }

        [Test]
        public void Database_schema_should_be_updated_if_requested()
        {
            var nhibernateProperties = SQLiteConfiguration.Standard
                .UsingFile(Path.GetTempFileName())
                .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
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