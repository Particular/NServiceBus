using System;
using NUnit.Framework;
using Raven.Abstractions.Exceptions;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class When_persisting_a_saga_with_the_same_unique_property_as_another_saga : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_enforce_uniqueness()
        {
            var saga1 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = "whatever"
                        };
            
            var saga2 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = "whatever"
                        };

            WithASagaPersistenceUnitOfWork(p => p.Save(saga1));
            Assert.Throws<InvalidOperationException>(() => WithASagaPersistenceUnitOfWork(p => p.Save(saga2)));
        }
    }
}