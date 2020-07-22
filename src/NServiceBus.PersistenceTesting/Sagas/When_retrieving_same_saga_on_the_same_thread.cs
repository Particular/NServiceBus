namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Persistence;

    [TestFixtureSource(typeof(SagaTestVariantSource), "Variants")]
    public class When_retrieving_same_saga_on_the_same_thread : SagaPersisterTests
    {
        [Test]
        public async Task Save_should_fail_when_data_changes_between_read_and_update()
        {
            configuration.RequiresOptimisticConcurrencySupport();
            var correlationPropertyData = Guid.NewGuid().ToString();

            var persister = configuration.SagaStorage;
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            Guid generatedSagaId;
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                var sagaData = new TestSagaData {SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow};
                await SaveSagaWithSession(sagaData, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
                generatedSagaId = sagaData.Id;
            }

            ContextBag losingContext;
            CompletableSynchronizedStorageSession losingSaveSession;
            TestSagaData staleRecord;

            var winningContext = configuration.GetContextBagForSagaStorage();
            var winningSaveSession = await configuration.SynchronizedStorage.OpenSession(winningContext);
            try
            {
                var record = await persister.Get<TestSagaData>(generatedSagaId, winningSaveSession, winningContext);

                losingContext = configuration.GetContextBagForSagaStorage();
                losingSaveSession = await configuration.SynchronizedStorage.OpenSession(losingContext);
                staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession, losingContext);

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
                Assert.ThrowsAsync<Exception>(async () =>
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

        public When_retrieving_same_saga_on_the_same_thread(TestVariant param) : base(param)
        {
        }
    }
}