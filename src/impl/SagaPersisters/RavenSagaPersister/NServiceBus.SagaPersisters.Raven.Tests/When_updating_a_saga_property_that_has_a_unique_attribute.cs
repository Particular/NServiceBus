using System;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class When_updating_a_saga_property_that_has_a_unique_attribute : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_not_allow_the_property_to_be_updated()
        {
            var saga1 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = "whatever1"
                        };

            SaveSaga(saga1);

            Assert.Throws<InvalidOperationException>(() => 
                UpdateSaga <SagaWithUniqueProperty>(saga1.Id, s => s.UniqueString = "whatever2"));
        }
    }
}