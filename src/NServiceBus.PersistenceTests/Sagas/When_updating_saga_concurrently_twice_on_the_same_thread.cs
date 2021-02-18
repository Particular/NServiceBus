namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Persistence;

    public class When_updating_saga_concurrently_twice_on_the_same_thread : SagaPersisterTests
    {
        [Test]
        public async Task Save_process_is_repeatable()
        {
            configuration.RequiresOptimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };
            await SaveSaga(saga);

            ContextBag losingContext1;
            CompletableSynchronizedStorageSession losingSaveSession1;
            TestSagaData staleRecord1;
            var persister = configuration.SagaStorage;

            var winningContext1 = configuration.GetContextBagForSagaStorage();
            var winningSaveSession1 = await configuration.SynchronizedStorage.OpenSession(winningContext1, default);

            try
            {
                var record1 = await persister.Get<TestSagaData>(saga.Id, winningSaveSession1, winningContext1, default);

                losingContext1 = configuration.GetContextBagForSagaStorage();
                losingSaveSession1 = await configuration.SynchronizedStorage.OpenSession(losingContext1, default);
                staleRecord1 = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession1, losingContext1, default);

                record1.DateTimeProperty = DateTime.UtcNow;
                await persister.Update(record1, winningSaveSession1, winningContext1, default);
                await winningSaveSession1.CompleteAsync(default);
            }
            finally
            {
                winningSaveSession1.Dispose();
            }

            try
            {
                Assert.That(async () =>
                {
                    await persister.Update(staleRecord1, losingSaveSession1, losingContext1, default);
                    await losingSaveSession1.CompleteAsync(default);
                }, Throws.InstanceOf<Exception>());
            }
            finally
            {
                losingSaveSession1.Dispose();
            }

            ContextBag losingContext2;
            CompletableSynchronizedStorageSession losingSaveSession2;
            TestSagaData staleRecord2;

            var winningContext2 = configuration.GetContextBagForSagaStorage();
            var winningSaveSession2 = await configuration.SynchronizedStorage.OpenSession(winningContext2, default);
            try
            {
                var record2 = await persister.Get<TestSagaData>(saga.Id, winningSaveSession2, winningContext2, default);

                losingContext2 = configuration.GetContextBagForSagaStorage();
                losingSaveSession2 = await configuration.SynchronizedStorage.OpenSession(losingContext2, default);
                staleRecord2 = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession2, losingContext2, default);

                record2.DateTimeProperty = DateTime.UtcNow;
                await persister.Update(record2, winningSaveSession2, winningContext2, default);
                await winningSaveSession2.CompleteAsync(default);
            }
            finally
            {
                winningSaveSession2.Dispose();
            }

            try
            {
                Assert.That(async () =>
                 {
                     await persister.Update(staleRecord2, losingSaveSession2, losingContext2, default);
                     await losingSaveSession2.CompleteAsync(default);
                 }, Throws.InstanceOf<Exception>());
            }
            finally
            {
                losingSaveSession2.Dispose();
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

        public When_updating_saga_concurrently_twice_on_the_same_thread(TestVariant param) : base(param)
        {
        }
    }
}