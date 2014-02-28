namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using NUnit.Framework;

    public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_persist_successfully()
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
            CompleteSaga<SagaWithUniqueProperty>(saga1.Id);
            SaveSaga(saga2);
        }
    }


    public class When_trying_to_fetch_a_non_existing_saga_by_its_unique_property : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_return_null()
        {
            WithASagaPersistenceUnitOfWork(p => Assert.Null(p.Get<SagaWithUniqueProperty>("UniqueString",
                                                                                          Guid.NewGuid().ToString())));
        }
    }
}