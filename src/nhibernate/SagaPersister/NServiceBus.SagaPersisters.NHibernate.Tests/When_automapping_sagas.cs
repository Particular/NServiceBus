using System;
using System.Linq;
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

          //var properties = SQLiteConfiguration.Standard
          //  .InMemory()
          //  .ToProperties();

          //File.WriteAllText("sqlite.txt", "");

          //foreach (var property in properties)
          //{
          //  File.AppendAllText("sqlite.txt", String.Format("{{ \"{0}\", \"{1}\" }}\n", property.Key, property.Value));

          //}

          var properties = SQLiteConfiguration.InMemory();

          sessionFactory = builder.Build(properties, false) as SessionFactoryImpl;

            persisterForTestSaga = sessionFactory.GetEntityPersisterFor<TestSaga>();
        }

      [Test]
        public void Id_generator_should_be_set_to_assigned()
        {
            Assert.AreEqual(persisterForTestSaga.IdentifierGenerator.GetType(),typeof(Assigned));
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
                .IdentifierGenerator.GetType(),typeof(GuidCombGenerator));
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
                .IdentifierGenerator.GetType(),typeof(IdentityGenerator));
        }


        [Test]
        public void Inherited_property_classes_should_be_mapped()
        {
            persisterForTestSaga.ShouldContainMappingsFor<PolymorpicPropertyBase>();

            sessionFactory.ShouldContainPersisterFor<PolymorpicProperty>();

        }

        [Test]
        public void Users_can_override_tablenames_by_using_an_attribute()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(TestSagaWithTableNameAttribute).FullName).ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;
            Assert.AreEqual(persister.TableName,"MyTestSchema_MyTestTable");
        }

        [Test]
        public void Users_can_override_tablenames_by_using_an_attribute_which_does_not_derive()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(DerivedFromTestSagaWithTableNameAttribute).FullName).ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;
            Assert.AreEqual(persister.TableName,"DerivedFromTestSagaWithTableNameAttribute");
        }

        [Test]
        public void Users_can_override_derived_tablenames_by_using_an_attribute()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(AlsoDerivedFromTestSagaWithTableNameAttribute).FullName).ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;
            Assert.AreEqual(persister.TableName,"MyDerivedTestTable");
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
    }

    public static class SessionFactoryExtensions
    {
        public static IEntityPersister GetEntityPersisterFor<T>(this SessionFactoryImpl sessionFactory)
        {
            return sessionFactory.GetEntityPersister(typeof(T).FullName);
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
                            .Any(x => x.Type.ReturnedClass == typeof(T)));

        }
    }
}