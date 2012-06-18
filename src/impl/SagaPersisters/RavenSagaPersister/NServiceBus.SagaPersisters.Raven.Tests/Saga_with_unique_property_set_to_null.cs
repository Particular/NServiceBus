using System;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class Saga_with_unique_property_set_to_null : Raven_saga_persistence_concern
    {
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void should_throw_a_ArgumentNullException()
        {
            string uniqueString = null;
            var saga1 = new SagaWithUniqueProperty
                            {
                                Id = Guid.NewGuid(),
                                UniqueString = uniqueString
                            };

            SaveSaga(saga1);        
        }
    }
}