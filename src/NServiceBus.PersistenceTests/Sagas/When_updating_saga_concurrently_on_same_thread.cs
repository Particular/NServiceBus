namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Persistence;

    public class When_updating_saga_concurrently_on_same_thread : SagaPersisterTests
    {
        [Test]
        public async Task Save_should_fail_when_data_changes_between_read_and_update()
        {
            configuration.RequiresOptimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var sagaData = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };
            await SaveSaga(sagaData);
            var generatedSagaId = sagaData.Id;

            ContextBag losingContext;
            CompletableSynchronizedStorageSession losingSaveSession;
            TestSagaData staleRecord;
            var persister = configuration.SagaStorage;

            var winningContext = configuration.GetContextBagForSagaStorage();
            using (var winningSaveSession = await configuration.SynchronizedStorage.OpenSession(winningContext, default))
            {
                var record = await persister.Get<TestSagaData>(generatedSagaId, winningSaveSession, winningContext, default);

                losingContext = configuration.GetContextBagForSagaStorage();
                losingSaveSession = await configuration.SynchronizedStorage.OpenSession(losingContext, default);
                staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession, losingContext, default);

                record.DateTimeProperty = DateTime.UtcNow;
                await persister.Update(record, winningSaveSession, winningContext, default);
                await winningSaveSession.CompleteAsync(default);
            }

            try
            {
                Assert.CatchAsync<Exception>(async () =>
                {
                    await persister.Update(staleRecord, losingSaveSession, losingContext, default);
                    await losingSaveSession.CompleteAsync(default);
                });
            }
            finally
            {
                losingSaveSession.Dispose();
            }
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

        public When_updating_saga_concurrently_on_same_thread(TestVariant param) : base(param)
        {
        }
    }
}