namespace NServiceBus.PersistenceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_persisting_the_same_saga_twice_in_two_sessions_on_the_same_thread : SagaPersisterTests<When_persisting_the_same_saga_twice_in_two_sessions_on_the_same_thread.TestSaga, When_persisting_the_same_saga_twice_in_two_sessions_on_the_same_thread.TestSagaData>
    {
        [Test]
        public async Task Save_process_is_repeatable()
        {
            configuration.RequiresOptimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };

            var persister = configuration.SagaStorage;
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                var correlationProperty = SetActiveSagaInstanceForSave(insertContextBag, new TestSaga(), saga);

                await persister.Save(saga, correlationProperty, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
            }

            ContextBag losingContext1;
            CompletableSynchronizedStorageSession losingSaveSession1;
            TestSagaData staleRecord1;

            var winningContext1 = configuration.GetContextBagForSagaStorage();
            var winningSaveSession1 = await configuration.SynchronizedStorage.OpenSession(winningContext1);

            try
            {
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext1, saga);
                var record1 = await persister.Get<TestSagaData>(saga.Id, winningSaveSession1, winningContext1);

                losingContext1 = configuration.GetContextBagForSagaStorage();
                losingSaveSession1 = await configuration.SynchronizedStorage.OpenSession(losingContext1);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingContext1, saga);
                staleRecord1 = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession1, losingContext1);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingContext1, staleRecord1);

                record1.DateTimeProperty = DateTime.UtcNow;
                await persister.Update(record1, winningSaveSession1, winningContext1);
                await winningSaveSession1.CompleteAsync();
            }
            finally
            {
                winningSaveSession1.Dispose();
            }

            try
            {
                await persister.Update(staleRecord1, losingSaveSession1, losingContext1);
                Assert.That(async () => await losingSaveSession1.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.EndsWith($"concurrency violation: saga entity Id[{saga.Id}] already saved."));
            }
            finally
            {
                losingSaveSession1.Dispose();
            }

            ContextBag losingContext2;
            CompletableSynchronizedStorageSession losingSaveSession2;
            TestSagaData staleRecord2;

            var winningContext2 = configuration.GetContextBagForSagaStorage();
            var winningSaveSession2 = await configuration.SynchronizedStorage.OpenSession(winningContext2);
            try
            {
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext2, saga);
                var record2 = await persister.Get<TestSagaData>(saga.Id, winningSaveSession2, winningContext2);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext2, record2);

                losingContext2 = configuration.GetContextBagForSagaStorage();
                losingSaveSession2 = await configuration.SynchronizedStorage.OpenSession(losingContext2);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingContext2, saga);
                staleRecord2 = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession2, losingContext2);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingContext2, staleRecord2);

                record2.DateTimeProperty = DateTime.UtcNow;
                await persister.Update(record2, winningSaveSession2, winningContext2);
                await winningSaveSession2.CompleteAsync();
            }
            finally
            {
                winningSaveSession2.Dispose();
            }

            try
            {
                await persister.Update(staleRecord2, losingSaveSession2, losingContext2);
                Assert.That(async () => await losingSaveSession2.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.EndsWith($"concurrency violation: saga entity Id[{saga.Id}] already saved."));
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
    }
}