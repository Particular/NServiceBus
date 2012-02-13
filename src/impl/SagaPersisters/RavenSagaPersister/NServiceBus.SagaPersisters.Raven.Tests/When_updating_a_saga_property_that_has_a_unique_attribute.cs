using System;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    using Saga;

    public class When_updating_a_saga_property_that_has_a_unique_attribute : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_allow_the_update()
        {
            var saga1 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = "whatever1"
                        };

            SaveSaga(saga1);

            UpdateSaga<SagaWithUniqueProperty>(saga1.Id, s => s.UniqueString = "whatever2");
         
            var saga2 = new SagaWithUniqueProperty
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever1"
            };

            //this should not blow since we changed the uniq value in the previous saga
            SaveSaga(saga2);
        }
    }
}