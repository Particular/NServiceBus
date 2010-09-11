using System;
using System.Linq;
using FluentNHibernate.Cfg.Db;
using NBehave.Spec.NUnit;
using NHibernate.ByteCode.LinFu;
using NHibernate.Id;
using NHibernate.Impl;
using NHibernate.Persister.Entity;
using NServiceBus.Saga;
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

            persisterForTestSaga = sessionFactory.GetEntityPersisterFor<TestSaga>();
        }

        [Test]
        public void Id_generator_should_be_set_to_assigned()
        {
            persisterForTestSaga.IdentifierGenerator.ShouldBeInstanceOfType(typeof(Assigned));
        }

        [Test]
        public void Enums_should_be_mapped_as_integers()
        {
            persisterForTestSaga.ShouldContainMappingsFor<StatusEnum>();
        }

        [Test]
        public void Related_entities_should_also_be_mapped()
        {
            sessionFactory.GetEntityPersisterFor<OrderLine>()
                .IdentifierGenerator.ShouldBeInstanceOfType(typeof(GuidCombGenerator));
        }

        [Test]
        public void Datetime_properties_should_be_mapped()
        {
            persisterForTestSaga.ShouldContainMappingsFor<DateTime>();
        }

        [Test]
        public void Public_setters_and_getters_of_concrete_classes_should_map_as_components()
        {
            persisterForTestSaga.ShouldContainMappingsFor<TestComponent>();
        }



        [Test]
        public void Users_can_override_automappings_by_embedding_hbm_files()
        {
            sessionFactory.GetEntityPersisterFor<TestSagaWithHbmlXmlOverride>()
                .IdentifierGenerator.ShouldBeInstanceOfType(typeof(IdentityGenerator));
        }


        [Test]
        public void Inherited_property_classes_should_be_mapped()
        {
            persisterForTestSaga.ShouldContainMappingsFor<PolymorpicPropertyBase>();

            sessionFactory.ShouldContainPersisterFor<PolymorpicProperty>();
        }
    }

    public static class SessionFactoryExtensions
    {
        public static IEntityPersister GetEntityPersisterFor<T>(this SessionFactoryImpl sessionFactory)
        {
            return sessionFactory.GetEntityPersister(typeof(T).FullName);
        }
        public static void ShouldContainPersisterFor<T>(this SessionFactoryImpl sessionFactory)
        {
            sessionFactory.GetEntityPersisterFor<T>().ShouldNotBeNull();
        }
    }

    public static class EntityPersisterExtensions
    {
        public static void ShouldContainMappingsFor<T>(this IEntityPersister persister)
        {
            persister.EntityMetamodel.Properties
                            .Any(x => x.Type.ReturnedClass == typeof(T))
                            .ShouldBeTrue();

        }
    }
}