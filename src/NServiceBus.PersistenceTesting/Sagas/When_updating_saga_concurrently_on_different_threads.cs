namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_updating_saga_concurrently_on_different_threads : SagaPersisterTests
    {
        [Test]
        public async Task Save_should_fail_when_data_changes_between_read_and_update_on_same_thread()
        {
            configuration.RequiresOptimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var sagaData = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };
            await SaveSaga(sagaData);
            var generatedSagaId = sagaData.Id;

            var startSecondTaskSync = new TaskCompletionSource<bool>();
            var firstTaskCanCompleteSync = new TaskCompletionSource<bool>();
            var persister = configuration.SagaStorage;

            var firstTask = Task.Run(async () =>
            {
                var winningContext = configuration.GetContextBagForSagaStorage();
                using (var winningSaveSession = await configuration.SynchronizedStorage.OpenSession(winningContext))
                {
                    var record = await persister.Get<TestSagaData>(generatedSagaId, winningSaveSession, winningContext);

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
                    var staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession, losingSaveContext);

                    firstTaskCanCompleteSync.SetResult(true);
                    await firstTask;

                    staleRecord.DateTimeProperty = DateTime.UtcNow.AddHours(1);
                    Assert.That(async () =>
                    {
                        await persister.Update(staleRecord, losingSaveSession, losingSaveContext);
                        await losingSaveSession.CompleteAsync();
                    }, Throws.InstanceOf<Exception>());
                }
            });

            await secondTask;
        }

        public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
            }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        public class TestSagaData : ContainSagaData
        {
            public string SomeId { get; set; } = "Test";

            public DateTime DateTimeProperty { get; set; }
        }

        public When_updating_saga_concurrently_on_different_threads(TestVariant param) : base(param)
        {
        }
    }
}