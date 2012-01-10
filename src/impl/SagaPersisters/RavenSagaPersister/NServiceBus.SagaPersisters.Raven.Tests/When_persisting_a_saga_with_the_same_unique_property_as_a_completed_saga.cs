using System;
using NUnit.Framework;
using Raven.Abstractions.Exceptions;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_persist_successfully()
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

            SagaPersister.Save(saga1);
            SagaPersister.Complete(saga1);

            SagaPersister.Save(saga2);
        }
    }
}