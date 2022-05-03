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
            ICompletableSynchronizedStorageSession losingSaveSession;
            TestSagaData staleRecord;
            var persister = configuration.SagaStorage;

            var winningContext = configuration.GetContextBagForSagaStorage();
            using (var winningSaveSession = configuration.CreateStorageSession())
            {
                await winningSaveSession.Open(winningContext);

                var record = await persister.Get<TestSagaData>(generatedSagaId, winningSaveSession, winningContext);

                losingContext = configuration.GetContextBagForSagaStorage();
                losingSaveSession = configuration.CreateStorageSession();
                await losingSaveSession.Open(losingContext);
                staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession, losingContext);

                record.DateTimeProperty = DateTime.UtcNow;
                await persister.Update(record, winningSaveSession, winningContext);
                await winningSaveSession.CompleteAsync();
            }

            try
            {
                Assert.CatchAsync<Exception>(async () =>
                {
                    await persister.Update(staleRecord, losingSaveSession, losingContext);
                    await losingSaveSession.CompleteAsync();
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