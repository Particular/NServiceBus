namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Config.Internal;
    using global::NHibernate.Cfg;
    using global::NHibernate.Impl;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class When_building_the_sessionFactory
    {
        private readonly IDictionary<string, string> testProperties = SQLiteConfiguration.InMemory();

        [Test]
        public void References_of_the_persistent_entity_should_also_be_mapped()
        {
            var assemblyContainingSagas = typeof (TestSaga).Assembly;
            var types = assemblyContainingSagas.GetTypes().ToList();
            types.Add(typeof(ContainSagaData));

            var builder = new SessionFactoryBuilder(types);

            var sessionFactory = builder.Build(new Configuration().AddProperties(testProperties)) as SessionFactoryImpl;

            Assert.NotNull(sessionFactory.GetEntityPersister(typeof (RelatedClass).FullName));
        }
    }
}