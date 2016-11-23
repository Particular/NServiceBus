namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using System.Transactions;
    using Transport;

    [TestFixture]
    class When_persisting_a_saga_with_InMemory_and_an_escalated_DTC_transaction
    {
        [Test]
        public async Task Save_fails_when_data_changes_between_concurrent_instances()
        {
            var sagaId = Guid.NewGuid();
            var saga = new TestSagaData
            {
                Id = sagaId,
                SomeId = sagaId.ToString()
            };

            var persister = new InMemorySagaPersister();
            var storageAdapter = new InMemoryTransactionalSynchronizedStorageAdapter();
            var insertSession = new InMemorySynchronizedStorageSession();

            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            Assert.That(async () =>
            {
                using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    Transaction.Current.EnlistDurable(EnlistmentWhichEnforcesDtcEscalation.Id, new EnlistmentWhichEnforcesDtcEscalation(), EnlistmentOptions.None);

                    var transportTransaction = new TransportTransaction();
                    transportTransaction.Set(Transaction.Current);

                    var unenlistedSession = new InMemorySynchronizedStorageSession();

                    var enlistedSession = await storageAdapter.TryAdapt(transportTransaction, new ContextBag());

                    var unenlistedRecord = await persister.Get<TestSagaData>(saga.Id, unenlistedSession, new ContextBag());
                    var enlistedRecord = await persister.Get<TestSagaData>("SomeId", sagaId.ToString(), enlistedSession, new ContextBag());

                    await persister.Update(unenlistedRecord, unenlistedSession, new ContextBag());
                    await persister.Update(enlistedRecord, enlistedSession, new ContextBag());

                    await unenlistedSession.CompleteAsync();

                    tx.Complete();
                }
            }, Throws.Exception.TypeOf<TransactionAbortedException>());
        }
    }

    public class EnlistmentWhichEnforcesDtcEscalation : IEnlistmentNotification
    {
        public static readonly Guid Id = Guid.NewGuid();

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}