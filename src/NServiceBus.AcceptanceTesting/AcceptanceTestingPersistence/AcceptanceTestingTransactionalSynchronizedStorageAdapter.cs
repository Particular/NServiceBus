namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using NServiceBus.Outbox;
    using Outbox;
    using Persistence;
    using Transport;

    public class AcceptanceTestingTransactionalSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            if (transaction is AcceptanceTestingOutboxTransaction inMemOutboxTransaction)
            {
                CompletableSynchronizedStorageSession session = new AcceptanceTestingSynchronizedStorageSession(inMemOutboxTransaction.Transaction);
                return Task.FromResult(session);
            }
            return EmptyTask;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            if (transportTransaction.TryGet(out Transaction ambientTransaction))
            {
                var transaction = new AcceptanceTestingTransaction();
                CompletableSynchronizedStorageSession session = new AcceptanceTestingSynchronizedStorageSession(transaction);
                ambientTransaction.EnlistVolatile(new EnlistmentNotification(transaction), EnlistmentOptions.None);
                return Task.FromResult(session);
            }
            return EmptyTask;
        }

        static readonly Task<CompletableSynchronizedStorageSession> EmptyTask = Task.FromResult<CompletableSynchronizedStorageSession>(null);

        class EnlistmentNotification : IEnlistmentNotification
        {
            public EnlistmentNotification(AcceptanceTestingTransaction transaction)
            {
                this.transaction = transaction;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                try
                {
                    transaction.Commit();
                    preparingEnlistment.Prepared();
                }
                catch (Exception ex)
                {
                    preparingEnlistment.ForceRollback(ex);
                }
            }

            public void Commit(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                transaction.Rollback();
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }

            AcceptanceTestingTransaction transaction;
        }
    }
}