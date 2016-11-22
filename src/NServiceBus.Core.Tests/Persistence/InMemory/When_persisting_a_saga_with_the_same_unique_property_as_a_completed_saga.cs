namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga
    {
        InMemorySagaPersister persister = new InMemorySagaPersister();

        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };
            var saga2 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };

            await SaveSaga(saga1);
            await CompleteSaga(saga1.Id);
            
            await SaveSaga(saga2);
            await CompleteSaga(saga2.Id);

            await SaveSaga(saga1);
            await CompleteSaga(saga1.Id);
        }

        async Task SaveSaga(SagaWithUniquePropertyData saga)
        {
            using (var session = new InMemorySynchronizedStorageSession())
            {
                var ctx = new ContextBag();
                await persister.Save(saga, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga), session, ctx);
                await session.CompleteAsync();
            }
        }

        async Task CompleteSaga(Guid sagaId)
        {
            using (var session = new InMemorySynchronizedStorageSession())
            {
                var ctx = new ContextBag();

                var saga = await persister.Get<SagaWithUniquePropertyData>(sagaId, session, ctx);
                await persister.Complete(saga, session, ctx);
                await session.CompleteAsync();
            }
        }
    }
}