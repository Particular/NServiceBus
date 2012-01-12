using System;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class When_updating_a_saga_with_the_same_unique_property_as_another_saga : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = "whatever1"
                        };
            
            var saga2 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = "whatever"
                        };

            WithASagaPersistenceUnitOfWork(p => p.Save(saga1));
            WithASagaPersistenceUnitOfWork(p => p.Save(saga2));

            Assert.Throws<InvalidOperationException>(() => WithASagaPersistenceUnitOfWork(p =>
            {
                var saga = p.Get<SagaWithUniqueProperty>(saga2.Id);
                saga.UniqueString = "whatever1";
                p.Update(saga);
            }));
        }
    }
}