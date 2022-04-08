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

    class AcceptanceTestingSynchronizedStorageSession : ICompletableSynchronizedStorageSession
    {
        public AcceptanceTestingTransaction Transaction { get; private set; }

        public void Dispose() => Transaction = null;

        public ValueTask<bool> OpenSession(IOutboxTransaction transaction, ContextBag context,
            CancellationToken cancellationToken = default)
        {
            if (transaction is AcceptanceTestingOutboxTransaction inMemOutboxTransaction)
            {
                Transaction = inMemOutboxTransaction.Transaction;
                ownsTransaction = false;
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(false);
        }

        public ValueTask<bool> OpenSession(TransportTransaction transportTransaction, ContextBag context,
            CancellationToken cancellationToken = default)
        {
            if (!transportTransaction.TryGet(out Transaction ambientTransaction))
            {
                return new ValueTask<bool>(false);
            }

            Transaction = new AcceptanceTestingTransaction();
            ambientTransaction.EnlistVolatile(new EnlistmentNotification(Transaction), EnlistmentOptions.None);
            ownsTransaction = true;
            return new ValueTask<bool>(true);
        }

        public ValueTask<bool> OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            ownsTransaction = true;
            Transaction = new AcceptanceTestingTransaction();
            return new ValueTask<bool>(true);
        }

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (ownsTransaction)
            {
                Transaction.Commit();
            }
            return Task.CompletedTask;
        }

        public void Enlist(Action action) => Transaction.Enlist(action);

        bool ownsTransaction;

        sealed class EnlistmentNotification : IEnlistmentNotification
        {
            public EnlistmentNotification(AcceptanceTestingTransaction transaction) => this.transaction = transaction;

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

            public void Commit(Enlistment enlistment) => enlistment.Done();

            public void Rollback(Enlistment enlistment)
            {
                transaction.Rollback();
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment) => enlistment.Done();

            readonly AcceptanceTestingTransaction transaction;
        }
    }
}