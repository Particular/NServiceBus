namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_completing_a_saga_loaded_by_id : SagaPersisterTests
    {
        [Test]
        public async Task Should_delete_the_saga()
        {
            var sagaId = Guid.NewGuid();

            var persister = configuration.SagaStorage;
            var insertContextBag = configuration.GetContextBagForSagaStorage();

            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                var saga = new TestSagaData { Id = sagaId, SomeId = sagaId.ToString() };
                var correlationProperty = SetActiveSagaInstance(insertContextBag, new TestSaga(), saga);

                await persister.Save(saga, correlationProperty, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
            }

            var intentionallySharedContext = configuration.GetContextBagForSagaStorage();
            TestSagaData sagaData;
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(intentionallySharedContext))
            {
                sagaData = await persister.Get<TestSagaData>(sagaId, completeSession, intentionallySharedContext);
                SetActiveSagaInstance(intentionallySharedContext, new TestSaga(), sagaData);

                await persister.Complete(sagaData, completeSession, intentionallySharedContext );
                await completeSession.CompleteAsync();
            }

            TestSagaData completedSaga;
            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                SetActiveSagaInstance(readContextBag, new TestSaga(), new TestSagaData());

                completedSaga = await persister.Get<TestSagaData>(sagaId, readSession, readContextBag);
            }

            Assert.NotNull(sagaData);
            Assert.Null(completedSaga);
        }
    }
}