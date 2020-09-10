namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Persistence;

    public class When_worker_tries_to_complete_saga_update_by_another_optimistic : SagaPersisterTests
    {
        [Test]
        public async Task Should_fail()
        {
            configuration.RequiresOptimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };

            await SaveSaga(saga);

            var persister = configuration.SagaStorage;

            ContextBag losingContext;
            CompletableSynchronizedStorageSession losingSaveSession;
            TestSagaData staleRecord;

            var winningContext = configuration.GetContextBagForSagaStorage();
            var winningSaveSession = await configuration.SynchronizedStorage.OpenSession(winningContext);
            try
            {
                var record = await persister.Get<TestSagaData>(saga.Id, winningSaveSession, winningContext, CancellationToken.None);

                losingContext = configuration.GetContextBagForSagaStorage();
                losingSaveSession = await configuration.SynchronizedStorage.OpenSession(losingContext);
                staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession, losingContext, CancellationToken.None);

                record.DateTimeProperty = DateTime.UtcNow;
                await persister.Update(record, winningSaveSession, winningContext, CancellationToken.None);
                await winningSaveSession.CompleteAsync();
            }
            finally
            {
                winningSaveSession.Dispose();
            }

            try
            {
                Assert.That(async () =>
                {
                    await persister.Complete(staleRecord, losingSaveSession, losingContext, CancellationToken.None);
                    await losingSaveSession.CompleteAsync();
                }, Throws.InstanceOf<Exception>());
            }
            finally
            {
                losingSaveSession.Dispose();
            }
        }

        public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
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

        public When_worker_tries_to_complete_saga_update_by_another_optimistic(TestVariant param) : base(param)
        {
        }
    }
}