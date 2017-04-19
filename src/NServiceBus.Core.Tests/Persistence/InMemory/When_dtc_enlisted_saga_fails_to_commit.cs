namespace NServiceBus.Core.Tests.Persistence.InMemory
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class When_dtc_enlisted_saga_fails_to_commit
    {
        [Test]
        public async Task Should_rollback_all_changes_in_transaction()
        {
            var persister = new InMemorySagaPersister();
            var storageAdapter = new InMemoryTransactionalSynchronizedStorageAdapter();

            var inMemorySynchronizedStorageSession = new InMemorySynchronizedStorageSession();

            // setup two sagas in the storage:
            var sagaData1 = new SagaData1
            {
                Id = Guid.NewGuid(),
                Content = "",
                CorrelationId = Guid.NewGuid()
            };
            await persister.Save(
                sagaData1,
                new SagaCorrelationProperty("CorrelationId", sagaData1.CorrelationId),
                inMemorySynchronizedStorageSession,
                new ContextBag());
            var sagaData2 = new SagaData2
            {
                Id = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid()
            };
            await persister.Save(
                sagaData2,
                new SagaCorrelationProperty("CorrelationId", sagaData2.CorrelationId),
                inMemorySynchronizedStorageSession,
                new ContextBag());
            await inMemorySynchronizedStorageSession.CompleteAsync();

            Assert.ThrowsAsync<TransactionAbortedException>(async () =>
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var transportTransaction = new TransportTransaction();
                    var contextBag = new ContextBag();
                    transportTransaction.Set(Transaction.Current);

                    var enlistedSession = await storageAdapter.TryAdapt(transportTransaction, new ContextBag());

                    // saga data needs to be fetched before calling update
                    var saga1 = await persister.Get<SagaData1>(nameof(SagaData1.CorrelationId), sagaData1.CorrelationId, enlistedSession, contextBag);
                    var saga2 = await persister.Get<SagaData2>(nameof(SagaData2.CorrelationId), sagaData2.CorrelationId, enlistedSession, contextBag);

                    saga1.Content = "Updated";

                    // update saga2 twice to provoke a concurrency issue
                    await persister.Update(saga1, enlistedSession, contextBag);
                    await persister.Update(saga2, enlistedSession, contextBag);
                    await persister.Update(saga2, enlistedSession, contextBag);

                    await enlistedSession.CompleteAsync();
                    scope.Complete();
                }
            }, "because it should fail due to conflicting update of saga2");

            var sagaData = await persister.Get<SagaData1>(nameof(SagaData1.CorrelationId), sagaData1.CorrelationId, new InMemorySynchronizedStorageSession(), new ContextBag());
            Assert.AreEqual(string.Empty, sagaData.Content, "because it should rollback all changes of the same transaction");
        }

        class SagaData1 : ContainSagaData
        {
            public Guid CorrelationId { get; set; }
            public string Content { get; set; }
        }

        class SagaData2 : ContainSagaData
        {
            public Guid CorrelationId { get; set; }
        }
    }
}