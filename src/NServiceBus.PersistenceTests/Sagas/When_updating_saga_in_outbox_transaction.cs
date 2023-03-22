namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_updating_saga_in_outbox_transaction : SagaPersisterTests
    {
        [Test]
        public async Task Should_save_saga_only_when_outbox_tx_committed()
        {
            configuration.RequiresOutboxSupport();
            configuration.RequiresOptimisticConcurrencySupport();

            var contextBag = configuration.GetContextBagForOutbox();

            var sagaData = new TestSagaData { SomeId = Guid.NewGuid().ToString() };
            using (var outboxTransaction = await configuration.OutboxStorage.BeginTransaction(contextBag))
            {
                using (var synchronizedStorageSession = configuration.CreateStorageSession())
                {
                    var sessionCreated = await synchronizedStorageSession.TryOpen(outboxTransaction, contextBag);
                    Assert.IsTrue(sessionCreated);

                    var readBeforeCreate = await configuration.SagaStorage.Get<TestSagaData>(nameof(TestSagaData.SomeId),
                        sagaData.SomeId, synchronizedStorageSession, contextBag);
                    Assert.IsNull(readBeforeCreate);

                    await SaveSagaWithSession(sagaData, synchronizedStorageSession, contextBag);

                    await synchronizedStorageSession.CompleteAsync();
                }

                // outbox transaction not yet committed
                var readBeforeOutboxCommit = await GetById<TestSagaData>(sagaData.Id);
                Assert.IsNull(readBeforeOutboxCommit);

                await outboxTransaction.Commit();
            }

            var readAfterOutboxCommit = await GetById<TestSagaData>(sagaData.Id);
            Assert.NotNull(readAfterOutboxCommit);
            Assert.AreEqual(sagaData.SomeId, readAfterOutboxCommit.SomeId);
        }

        public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartTestSagaMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper) => mapper.ConfigureMapping<StartTestSagaMessage>(m => m.SomeId).ToSaga(s => s.SomeId);

            public Task Handle(StartTestSagaMessage message, IMessageHandlerContext context) => throw new NotImplementedException();
        }

        public class TestSagaData : ContainSagaData
        {
            public string SomeId { get; set; } = "Test";
        }

        public class StartTestSagaMessage : IMessage
        {
            public string SomeId { get; set; }
        }

        public When_updating_saga_in_outbox_transaction(TestVariant param) : base(param)
        {
        }
    }
}