namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config.Internal;
    using NUnit.Framework;
    using Saga;
    using global::NHibernate.Cfg;
    using global::NHibernate.Impl;

    [TestFixture]
    public class When_automapping_sagas_with_nested_types
    {
        private SessionFactoryImpl sessionFactory;

        [SetUp]
        public void SetUp()
        {
            var assemblyContainingSagas = typeof(TestSaga).Assembly;
            var types = assemblyContainingSagas.GetTypes().ToList();
            types.Add(typeof (ContainSagaData));

            var builder = new SessionFactoryBuilder(types);
            var properties = SQLiteConfiguration.InMemory();

            sessionFactory = builder.Build(new Configuration().AddProperties(properties)) as SessionFactoryImpl;
        }

        [Test]
        public void Table_name_for_nested_entitiy_should_be_generated_correctly()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(SagaWithNestedType.Customer).FullName).
                   ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;

            Assert.AreEqual( "SagaWithNestedType_Customer", persister.TableName);
        }

        [Test]
        public void Table_name_for_nested_saga_data_should_be_the_parent_saga()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(SagaWithNestedSagaData.NestedSagaData).FullName).
                   ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;

            Assert.AreEqual("SagaWithNestedSagaData", persister.TableName);
        }

        [Test]
        public void Table_name_for_nested_saga_data_that_derives_from_abstract_class_should_be_the_parent_saga()
        {
            var persister = sessionFactory.GetEntityPersister(typeof(TestSaga2).FullName).
                   ClassMetadata as global::NHibernate.Persister.Entity.AbstractEntityPersister;

            Assert.AreEqual("When_automapping_sagas_with_nested_types", persister.TableName);
        }

        public class TestSaga2 : ContainSagaData
        {

        }
    }

    public class SagaWithNestedType : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }


        public virtual IList<Customer> Customers { get; set; }

        public class Customer
        {
            public virtual Guid Id { get; set; }
            public virtual string Name { get; set; }
        }
    }

    public class SagaWithNestedSagaData
    {
        public class NestedSagaData : IContainSagaData
        {
            public virtual Guid Id { get; set; }
            public virtual string Originator { get; set; }
            public virtual string OriginalMessageId { get; set; }

        } 
    }
}