using System;
using NHibernate.Exceptions;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.NHibernate.Tests
{
    [TestFixture]
    public class When_persisting_a_saga_with_a_unique_property : InMemoryFixture
    {
        [Test]
        public void The_database_should_enforce_the_uniqueness()
        {
            UnitOfWork.Begin();
            SagaPersister.Save(new SagaWithUniqueProperty
                                   {
                                       Id = Guid.NewGuid(),
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

    public class SagaWithUniqueProperty : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }
    }
}