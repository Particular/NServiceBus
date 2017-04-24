// ReSharper disable AccessToDisposedClosure
namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_multiple_workers_retrieve_same_saga_on_different_threads : SagaPersisterTests
    {
        [Test]
        public async Task Save_fails_when_data_changes_between_read_and_update_on_same_thread()
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

            var startSecondTaskSync = new TaskCompletionSource<bool>();
            var firstTaskCanCompleteSync = new TaskCompletionSource<bool>();

            var firstTask = Task.Run(async () =>
            {
                var winningContext = configuration.GetContextBagForSagaStorage();
                using (var winningSaveSession = await configuration.SynchronizedStorage.OpenSession(winningContext))
                {
                    SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext, new TestSagaData { Id = generatedSagaId, SomeId = correlationPropertyData });
                    var record = await persister.Get<TestSagaData>(generatedSagaId, winningSaveSession, winningContext);
                    SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext, record);

                    startSecondTaskSync.SetResult(true);
                    await firstTaskCanCompleteSync.Task;

                    record.DateTimeProperty = DateTime.UtcNow;
                    await persister.Update(record, winningSaveSession, winningContext);
                    await winningSaveSession.CompleteAsync();
                }
            });

            var secondTask = Task.Run(async () =>
            {
                await startSecondTaskSync.Task;

                var losingSaveContext = configuration.GetContextBagForSagaStorage();
                using (var losingSaveSession = await configuration.SynchronizedStorage.OpenSession(losingSaveContext))
                {
                    SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingSaveContext, new TestSagaData { Id = generatedSagaId, SomeId = correlationPropertyData });
                    var staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession, losingSaveContext);
                    SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingSaveContext, staleRecord);

                    firstTaskCanCompleteSync.SetResult(true);
                    await firstTask;

                    staleRecord.DateTimeProperty = DateTime.UtcNow.AddHours(1);
                    await persister.Update(staleRecord, losingSaveSession, losingSaveContext);
                    Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.EndsWith($"concurrency violation: saga entity Id[{generatedSagaId}] already saved."));
                }
            });

            await secondTask;
        }
    }
}