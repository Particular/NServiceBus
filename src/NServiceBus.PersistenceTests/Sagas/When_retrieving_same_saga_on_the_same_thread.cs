namespace NServiceBus.PersistenceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
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
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext, new TestSagaData {Id = generatedSagaId, SomeId = correlationPropertyData});
                var record = await persister.Get<TestSagaData>(generatedSagaId, winningSaveSession, winningContext);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext, record);

                losingContext = configuration.GetContextBagForSagaStorage();
                losingSaveSession = await configuration.SynchronizedStorage.OpenSession(losingContext);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingContext, new TestSagaData {Id = generatedSagaId, SomeId = correlationPropertyData});
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
    }
}