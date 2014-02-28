namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using NUnit.Framework;

    public class Saga_with_unique_property_set_to_null : Raven_saga_persistence_concern
    {
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void should_throw_a_ArgumentNullException()
        {
            var saga1 = new SagaWithUniqueProperty
                            {
                                Id = Guid.NewGuid(),
                                UniqueString = null
                            };

            SaveSaga(saga1);        
        }
    }
}