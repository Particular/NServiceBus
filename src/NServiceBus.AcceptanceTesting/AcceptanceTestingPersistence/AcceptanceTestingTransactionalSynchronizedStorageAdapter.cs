namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Transport;

    class AcceptanceTestingTransactionalSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is AcceptanceTestingOutboxTransaction inMemOutboxTransaction)
            {
                ICompletableSynchronizedStorageSession session = new AcceptanceTestingSynchronizedStorageSession(inMemOutboxTransaction.Transaction);
                return Task.FromResult(session);
            }
            return EmptyTask;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transportTransaction.TryGet(out Transaction ambientTransaction))
            {
                var transaction = new AcceptanceTestingTransaction();
                ICompletableSynchronizedStorageSession session = new AcceptanceTestingSynchronizedStorageSession(transaction);
                ambientTransaction.EnlistVolatile(new EnlistmentNotification(transaction), EnlistmentOptions.None);
                return Task.FromResult(session);
            }
            return EmptyTask;
        }

        static readonly Task<ICompletableSynchronizedStorageSession> EmptyTask = Task.FromResult<ICompletableSynchronizedStorageSession>(null);

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