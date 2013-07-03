namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using Config.Internal;
    using NUnit.Framework;
    using Saga;
    using global::NHibernate.Cfg;
    using global::NHibernate.Impl;

    [TestFixture]
    public class When_automapping_sagas_with_abstract_base_class
    {
        private SessionFactoryImpl sessionFactory;

        [SetUp]
        public void SetUp()
        {
            var builder = new SessionFactoryBuilder(new[] { typeof(SagaWithAbstractBaseClass), typeof(ContainSagaData), typeof(MyOwnAbstractBase) });

            var properties = SQLiteConfiguration.InMemory();

            sessionFactory = builder.Build(new Configuration().AddProperties(properties)) as SessionFactoryImpl;

        }

        [Test]
        public void Should_not_generate_join_table_for_base_class()
        {

            var persister = sessionFactory.GetEntityPersister(typeof(SagaWithAbstractBaseClass).FullName).
                   ClassMetadata as global::NHibernate.Persister.Entity.UnionSubclassEntityPersister;

            Assert.NotNull(persister, "The persister should be single table");
        }
    }

    public class SagaWithAbstractBaseClass : MyOwnAbstractBase
    {
        public virtual Guid OrderId { get; set; }
    }

    public abstract class MyOwnAbstractBase : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }
    }
   
}