namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System.Collections.Generic;
    using Config.Internal;
    using NUnit.Framework;
    using global::NHibernate.Cfg;
    using global::NHibernate.Impl;

    [TestFixture]
    public class When_building_the_sessionfactory
    {
        private readonly IDictionary<string, string> testProperties = SQLiteConfiguration.InMemory();

        [Test]
        public void References_of_the_persistent_entity_should_also_be_mapped()
        {
            var assemblyContainingSagas = typeof (TestSaga).Assembly;

            var builder = new SessionFactoryBuilder(assemblyContainingSagas.GetTypes());

            var sessionFactory = builder.Build(new Configuration().AddProperties(testProperties)) as SessionFactoryImpl;

            Assert.NotNull(sessionFactory.GetEntityPersister(typeof (RelatedClass).FullName));
        }
    }
}