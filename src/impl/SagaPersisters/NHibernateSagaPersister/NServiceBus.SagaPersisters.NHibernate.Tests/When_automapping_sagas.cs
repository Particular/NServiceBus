using System.Linq;
using FluentNHibernate.Cfg.Db;
using NBehave.Spec.NUnit;
using NHibernate.ByteCode.LinFu;
using NHibernate.Id;
using NHibernate.Impl;
using NHibernate.Persister.Entity;
using NServiceBus.SagaPersisters.NHibernate.Config.Internal;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_automapping_sagas
    {
        private IEntityPersister persisterForTestSaga;
        private SessionFactoryImpl sessionFactory;

        [SetUp]
        public void SetUp()
        {
            var assemblyContainingSagas = typeof(TestSaga).Assembly;

            var builder = new SessionFactoryBuilder(assemblyContainingSagas.GetTypes());

            sessionFactory = builder.Build(SQLiteConfiguration.Standard
             .InMemory()
             .ProxyFactoryFactory(typeof(ProxyFactoryFactory).AssemblyQualifiedName)
             .ToProperties(), false) as SessionFactoryImpl;

            persisterForTestSaga = sessionFactory.GetEntityPersister(typeof(TestSaga).FullName);

            persisterForTestSaga.ShouldNotBeNull();
        }

        [Test]
        public void Id_generator_should_be_set_to_assigned()
        {
            persisterForTestSaga.IdentifierGenerator.ShouldBeInstanceOfType(typeof(Assigned));
        }

        [Test]
        public void Enums_should_be_mapped_as_integers()
        {
            persisterForTestSaga.EntityMetamodel.Properties
                            .Where(x => x.Type.ReturnedClass == typeof(StatusEnum))
                            .Count().ShouldEqual(1);
        }

        [Test]
        public void Related_entities_should_also_be_mapped()
        {
            var persisterForOrderLine = sessionFactory.GetEntityPersister(typeof(OrderLine).FullName);

            persisterForOrderLine.IdentifierGenerator.ShouldBeInstanceOfType(typeof(GuidCombGenerator));
        }

        [Test]
        public void Datetime_properties_should_be_mapped()
        {
            var dateTimeProperty = persisterForTestSaga.EntityMetamodel.Properties
                .Where(x => x.Name == "DateTimeProperty")
                .FirstOrDefault();

            dateTimeProperty.ShouldNotBeNull();
        }

        [Test]
        public void Public_setters_and_getters_of_concrete_classes_should_map_as_components()
        {
            persisterForTestSaga.EntityMetamodel.Properties
                                       .Where(x => x.Type.ReturnedClass == typeof(TestComponent))
                                       .Count().ShouldEqual(1);
        }



        [Test,Ignore("TODO")]
        public void Users_can_override_automappings_by_embedding_hbm_files()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(TestSagaWithHbmlXmlOverride).FullName);
            persister.IdentifierGenerator.ShouldBeInstanceOfType(typeof(IdentityGenerator));

            
        }


    }
}