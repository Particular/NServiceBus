namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using NUnit.Framework;

    public class When_storing_a_saga_with_a_long_namespace : Raven_saga_persistence_concern
    {
        [Test]
        public void Should_not_generate_a_to_long_unique_property_id()
        {
            var uniqueString = Guid.NewGuid().ToString();
            var saga1 = new SagaWithUniquePropertyAndALongNamespace
                            {
                                Id = Guid.NewGuid(),
                                UniqueString = uniqueString
                            };

            SaveSaga(saga1);

          
        }
    }
}