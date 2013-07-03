namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Linq;
    using Config.Internal;
    using NUnit.Framework;
    using Saga;
    using global::NHibernate.Cfg;
    using global::NHibernate.Engine;
    using global::NHibernate.Id;
    using global::NHibernate.Impl;
    using global::NHibernate.Persister.Entity;

    [TestFixture]
    public class When_automapping_sagas
    {
        private IEntityPersister persisterForTestSaga;
        private SessionFactoryImpl sessionFactory;

        [SetUp]
        public void SetUp()
        {
            var assemblyContainingSagas = typeof (TestSaga).Assembly;
            var types = assemblyContainingSagas.GetTypes().ToList();
            types.Add(typeof(ContainSagaData));

            var builder = new SessionFactoryBuilder(types);
            var properties = SQLiteConfiguration.InMemory();

            sessionFactory = builder.Build(new Configuration().AddProperties(properties)) as SessionFactoryImpl;

            persisterForTestSaga = sessionFactory.GetEntityPersisterFor<TestSaga>();
        }

        [Test]
        public void Id_generator_should_be_set_to_assigned()
        {
            Assert.AreEqual(persisterForTestSaga.IdentifierGenerator.GetType(), typeof (Assigned));
        }

        [Test]
        public void Enums_should_be_mapped_as_integers()
        {
            persisterForTestSaga.ShouldContainMappingsFor<StatusEnum>();
        }

        [Test]
        public void Related_entities_should_also_be_mapped()
        {
            Assert.AreEqual(sessionFactory.GetEntityPersisterFor<OrderLine>()
                                .IdentifierGenerator.GetType(), typeof (GuidCombGenerator));
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
            Assert.AreEqual(sessionFactory.GetEntityPersisterFor<TestSagaWithHbmlXmlOverride>()
                                .IdentifierGenerator.GetType(), typeof (IdentityGenerator));
        }


        [Test,Ignore("Not supported any more")]
        public void Inherited_property_classes_should_be_mapped()
        {
            persisterForTestSaga.ShouldContainMappingsFor<PolymorpicPropertyBase>();

            sessionFactory.ShouldContainPersisterFor<PolymorpicProperty>();

        }

        [Test]
        public void Users_can_override_tablenames_by_using_an_attribute()
        {
            var persister =
                sessionFactory.GetEntityPersister(typeof (TestSagaWithTableNameAttribute).FullName).ClassMetadata as
                global::NHibernate.Persister.Entity.AbstractEntityPersister;
            Assert.AreEqual(persister.RootTableName, "MyTestSchema_MyTestTable");
        }

        [Test]
        public void Users_can_override_tablenames_by_using_an_attribute_which_does_not_derive()
        {
            var persister =
                sessionFactory.GetEntityPersister(typeof (DerivedFromTestSagaWithTableNameAttribute).FullName).
                    ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;
            Assert.AreEqual(persister.TableName, "DerivedFromTestSagaWithTableNameAttribute");
        }

        [Test]
        public void Users_can_override_derived_tablenames_by_using_an_attribute()
        {
            var persister =
                sessionFactory.GetEntityPersister(typeof (AlsoDerivedFromTestSagaWithTableNameAttribute).FullName).
                    ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;
            Assert.AreEqual(persister.TableName, "MyDerivedTestTable");
        }

        [Test]
        public void Array_of_ints_should_be_mapped_as_serializable()
        {
            var p = persisterForTestSaga.EntityMetamodel.Properties.SingleOrDefault(x => x.Name == "ArrayOfInts");
            Assert.IsNotNull(p);

            Assert.AreEqual(global::NHibernate.NHibernateUtil.Serializable.GetType(), p.Type.GetType());
        }

        [Test]
        public void Array_of_string_should_be_mapped_as_serializable()
        {
            var p = persisterForTestSaga.EntityMetamodel.Properties.SingleOrDefault(x => x.Name == "ArrayOfStrings");
            Assert.IsNotNull(p);

            Assert.AreEqual(global::NHibernate.NHibernateUtil.Serializable.GetType(), p.Type.GetType());
        }

        [Test]
        public void Array_of_dates_should_be_mapped_as_serializable()
        {
            var p = persisterForTestSaga.EntityMetamodel.Properties.SingleOrDefault(x => x.Name == "ArrayOfDates");
            Assert.IsNotNull(p);

            Assert.AreEqual(global::NHibernate.NHibernateUtil.Serializable.GetType(), p.Type.GetType());
        }

        [Test]
        public void Versioned_Property_should_override_optimistic_lock()
        {

          var persister1 = sessionFactory.GetEntityPersisterFor<SagaWithVersionedPropertyAttribute>();
          var persister2 = sessionFactory.GetEntityPersisterFor<SagaWithoutVersionedPropertyAttribute>();
        
          Assert.True(persister1.IsVersioned);
          Assert.False(persister1.EntityMetamodel.IsDynamicUpdate);
          Assert.AreEqual(Versioning.OptimisticLock.Version, persister1.EntityMetamodel.OptimisticLockMode);

          Assert.True(persister2.EntityMetamodel.IsDynamicUpdate);
          Assert.AreEqual(Versioning.OptimisticLock.All, persister2.EntityMetamodel.OptimisticLockMode);
          Assert.False(persister2.IsVersioned);
        }
    }

    public static class SessionFactoryExtensions
    {
        public static IEntityPersister GetEntityPersisterFor<T>(this SessionFactoryImpl sessionFactory)
        {
            return sessionFactory.GetEntityPersister(typeof (T).FullName);
        }

        public static void ShouldContainPersisterFor<T>(this SessionFactoryImpl sessionFactory)
        {
            Assert.NotNull(sessionFactory.GetEntityPersisterFor<T>());
        }
    }

    public static class EntityPersisterExtensions
    {
        public static void ShouldContainMappingsFor<T>(this IEntityPersister persister)
        {
            Assert.True(persister.EntityMetamodel.Properties
                            .Any(x => x.Type.ReturnedClass == typeof (T)));

        }
    }
}