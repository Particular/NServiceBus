using System;
using NUnit.Framework;
using Raven.Abstractions.Exceptions;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class When_persisting_a_saga_with_the_same_unique_property_as_the_original_value_of_another_saga_before_updating : Raven_saga_persistence_concern
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
            saga1 = SagaPersister.Get<SagaWithUniqueProperty>(saga1.Id);
            saga1.UniqueString = "whatever2";
            SagaPersister.Update(saga1);
            SagaPersister.Save(saga2);
        }
    }
}