namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    using System;
    using global::NHibernate.Exceptions;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class When_persisting_a_saga_with_a_unique_property : InMemoryFixture
    {
        [Test]
        public void The_database_should_enforce_the_uniqueness()
        {
            UnitOfWork.Begin();
            
            var id = Guid.NewGuid();

            ((ISagaPersister)SagaPersister).Get<SagaWithUniqueProperty>("UniqueString","whatever");

            SagaPersister.Save(new SagaWithUniqueProperty
                                   {
                                       Id = id,
                                       UniqueString = "whatever"
                                   });

            SagaPersister.Save(new SagaWithUniqueProperty
                                   {
                                       Id = Guid.NewGuid(),
                                       UniqueString = "whatever"
                                   });

            Assert.Throws<GenericADOException>(() => UnitOfWork.End());
        }
    }

    [LockMode(LockModes.None)]
    public class SagaWithUniqueProperty : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }
    }

    
}