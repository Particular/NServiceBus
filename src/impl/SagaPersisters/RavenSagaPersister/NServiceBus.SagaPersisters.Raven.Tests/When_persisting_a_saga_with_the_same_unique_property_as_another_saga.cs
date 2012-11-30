using System;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    using Persistence;
    using Persistence.Raven;

    public class When_persisting_a_saga_with_the_same_unique_property_as_another_saga : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_enforce_uniqueness()
        {
            var uniqueString = Guid.NewGuid().ToString();

            var saga1 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = uniqueString
                        };
            
            var saga2 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = uniqueString
                        };

            SaveSaga(saga1);
            Assert.Throws<ConcurrencyException>(() => SaveSaga(saga2));
        }
    }
}