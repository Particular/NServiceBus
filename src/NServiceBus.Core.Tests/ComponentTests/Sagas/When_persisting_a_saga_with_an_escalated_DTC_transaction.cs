namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class When_persisting_a_saga_with_an_escalated_DTC_transaction : SagaPersisterTests
    {
        [Test]
        public async Task Save_fails_when_data_changes_between_concurrent_instances()
        {
            configuration.RequiresDtcSupport();

            var sagaId = Guid.NewGuid();
            var saga = new TestSagaData { Id = sagaId, SomeId = sagaId.ToString() };

            var persister = configuration.SagaStorage;
            var savingContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                SetActiveSagaInstance(savingContextBag, new TestSaga(), saga);

                await persister.Save(saga, null, session, savingContextBag);
                await session.CompleteAsync();
            }

            Assert.That(async () =>
            {
                var storageAdapter = configuration.SynchronizedStorageAdapter;
                using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    Transaction.Current.EnlistDurable(EnlistmentWhichEnforcesDtcEscalation.Id, new EnlistmentWhichEnforcesDtcEscalation(), EnlistmentOptions.None);

                    var transportTransaction = new TransportTransaction();
                    transportTransaction.Set(Transaction.Current);

                    var unenlistedContextBag = configuration.GetContextBagForSagaStorage();
                    var unenlistedSession = await configuration.SynchronizedStorage.OpenSession(unenlistedContextBag);

                    var enlistedContextBag = configuration.GetContextBagForSagaStorage();
                    var enlistedSession = await storageAdapter.TryAdapt(transportTransaction, enlistedContextBag);

                    var unenlistedRecord = await persister.Get<TestSagaData>(saga.Id, unenlistedSession, unenlistedContextBag);
                    SetActiveSagaInstance(unenlistedContextBag, new TestSaga(), unenlistedRecord);

                    var enlistedRecord = await persister.Get<TestSagaData>("Id", saga.Id, enlistedSession, enlistedContextBag);
                    SetActiveSagaInstance(enlistedContextBag, new TestSaga(), enlistedRecord);

                    await persister.Update(unenlistedRecord, unenlistedSession, unenlistedContextBag);
                    await persister.Update(enlistedRecord, enlistedSession, enlistedContextBag);

                    await unenlistedSession.CompleteAsync();

                    tx.Complete();
                }
            }, Throws.Exception.TypeOf<TransactionAbortedException>());
        }

        class EnlistmentWhichEnforcesDtcEscalation : IEnlistmentNotification
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
}