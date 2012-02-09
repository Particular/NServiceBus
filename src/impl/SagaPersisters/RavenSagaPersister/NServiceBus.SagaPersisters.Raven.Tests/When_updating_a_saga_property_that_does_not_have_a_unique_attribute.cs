using System;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class When_updating_a_saga_property_that_does_not_have_a_unique_attribute : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = "whatever",
                            NonUniqueString = "notUnique"
                        };

            SaveSaga(saga1);

            UpdateSaga<SagaWithUniqueProperty>(saga1.Id, s => s.NonUniqueString = "notUnique2");
        }
    }
}