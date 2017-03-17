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
            var sagaId = Guid.NewGuid();
            var saga = new TestSagaData { Id = sagaId, SomeId = sagaId.ToString() };

            var persister = configuration.SagaStorage;
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                var correlationProperty = SetActiveSagaInstance(insertContextBag, new TestSaga(), saga);

                await persister.Save(saga, correlationProperty, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
            }

            var startSecondTaskSync = new TaskCompletionSource<bool>();
            var firstTaskCanCompleteSync = new TaskCompletionSource<bool>();

            var firstTask = Task.Run(async () =>
            {
                var winningContext = configuration.GetContextBagForSagaStorage();
                var winningSaveSession = await configuration.SynchronizedStorage.OpenSession(winningContext);
                var record = await persister.Get<TestSagaData>(saga.Id, winningSaveSession, winningContext);

                startSecondTaskSync.SetResult(true);
                await firstTaskCanCompleteSync.Task;

                record.DateTimeProperty = DateTime.Now;
                await persister.Update(record, winningSaveSession, winningContext);
                await winningSaveSession.CompleteAsync();
                winningSaveSession.Dispose();

            });

            var secondTask = Task.Run(async () =>
            {
                await startSecondTaskSync.Task;

                var losingSaveContext = configuration.GetContextBagForSagaStorage();
                var losingSaveSession = await configuration.SynchronizedStorage.OpenSession(losingSaveContext);
                var staleRecord = await persister.Get<TestSagaData>("SomeId", sagaId.ToString(), losingSaveSession, losingSaveContext);

                firstTaskCanCompleteSync.SetResult(true);
                await firstTask;

                staleRecord.DateTimeProperty = DateTime.Now.AddHours(1);
                await persister.Update(staleRecord, losingSaveSession, losingSaveContext);
                Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.EndsWith($"concurrency violation: saga entity Id[{saga.Id}] already saved."));
            });

            await secondTask;
        }
    }
}