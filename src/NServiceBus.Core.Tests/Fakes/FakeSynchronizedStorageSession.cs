namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using NServiceBus.Persistence;
    using Outbox;
    using Transport;

    public class FakeSynchronizedStorageSession : ICompletableSynchronizedStorageSession
    {
        public FakeSynchronizedStorageSession(FakeTransaction transaction)
        {
            Transaction = transaction;
        }

        public FakeSynchronizedStorageSession()
            : this(new FakeTransaction())
        {
            ownsTransaction = true;
        }

        public FakeTransaction Transaction { get; private set; }

        public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context,
            CancellationToken cancellationToken = default)
        {
            if (transaction is FakeOutboxTransaction inMemOutboxTransaction)
            {
                Transaction = inMemOutboxTransaction.Transaction;
                ownsTransaction = false;
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(false);
        }

        public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context,
            CancellationToken cancellationToken = default)
        {
            if (!transportTransaction.TryGet(out Transaction ambientTransaction))
            {
                return new ValueTask<bool>(false);
            }

            Transaction = new FakeTransaction();
            ambientTransaction.EnlistVolatile(new EnlistmentNotification(Transaction), EnlistmentOptions.None);
            ownsTransaction = true;
            return new ValueTask<bool>(true);
        }

        public Task Open(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            ownsTransaction = true;
            Transaction = new FakeTransaction();
            return Task.CompletedTask;
        }

        public void Dispose() => Transaction = null;

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (ownsTransaction)
            {
                Transaction.Commit();
            }
            return Task.CompletedTask;
        }

        public void Enlist(Action action)
        {
            Transaction.Enlist(action);
        }

        bool ownsTransaction;

        sealed class EnlistmentNotification : IEnlistmentNotification
        {
            public EnlistmentNotification(FakeTransaction transaction) => this.transaction = transaction;

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

            readonly FakeTransaction transaction;
        }
    }
}