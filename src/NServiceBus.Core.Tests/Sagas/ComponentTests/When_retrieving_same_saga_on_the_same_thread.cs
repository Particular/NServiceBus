// ReSharper disable AccessToDisposedClosure
namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class When_retrieving_same_saga_on_the_same_thread : SagaPersisterTests
    {
        [Test]
        public async Task Save_should_fails_when_data_changes_between_read_and_update()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();

            var persister = configuration.SagaStorage;
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            Guid generatedSagaId;
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                var sagaData = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };
                var correlationProperty = SetActiveSagaInstanceForSave(insertContextBag, new TestSaga(), sagaData);
                generatedSagaId = sagaData.Id;

                await persister.Save(sagaData, correlationProperty, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
            }

            ContextBag losingContext;
            CompletableSynchronizedStorageSession losingSaveSession;
            TestSagaData staleRecord;

            var winningContext = configuration.GetContextBagForSagaStorage();
            var winningSaveSession = await configuration.SynchronizedStorage.OpenSession(winningContext);
            try
            {
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext, new TestSagaData { Id = generatedSagaId, SomeId = correlationPropertyData });
                var record = await persister.Get<TestSagaData>(generatedSagaId, winningSaveSession, winningContext);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext, record);

                losingContext = configuration.GetContextBagForSagaStorage();
                losingSaveSession = await configuration.SynchronizedStorage.OpenSession(losingContext);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingContext, new TestSagaData { Id = generatedSagaId, SomeId = correlationPropertyData });
                staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession, losingContext);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingContext, staleRecord);

                record.DateTimeProperty = DateTime.UtcNow;
                await persister.Update(record, winningSaveSession, winningContext);
                await winningSaveSession.CompleteAsync();
            }
            finally
            {
                winningSaveSession.Dispose();
            }

            try
            {
                await persister.Update(staleRecord, losingSaveSession, losingContext);
                Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.EndsWith($"concurrency violation: saga entity Id[{generatedSagaId}] already saved."));
            }
            finally
            {
                losingSaveSession.Dispose();
            }
        }
    }
}