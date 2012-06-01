using System;
using System.Collections.Generic;
using System.IO;
using NHibernate.Impl;
using NServiceBus.SagaPersisters.NHibernate.Config.Internal;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_building_the_sessionfactory
    {
      private readonly IDictionary<string, string> testProperties = SQLiteConfiguration.InMemory();
       
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
          var nhibernateProperties = SQLiteConfiguration.UsingFile(Path.GetTempFileName());

          File.WriteAllText("sqlite.txt", "");

          foreach (var property in nhibernateProperties)
          {
            File.AppendAllText("sqlite.txt", String.Format("{{ \"{0}\", \"{1}\" }}\n", property.Key, property.Value));

          }

            var sessionFactory = new SessionFactoryBuilder(typeof(TestSaga).Assembly.GetTypes())
                .Build(nhibernateProperties, true);

            using (var session = sessionFactory.OpenSession())
            {
                session.CreateCriteria(typeof(TestSaga)).List<TestSaga>();
            }
        }
    }
}