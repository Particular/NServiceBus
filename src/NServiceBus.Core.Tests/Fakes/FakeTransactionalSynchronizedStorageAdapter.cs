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

    public class FakeTransactionalSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is FakeOutboxTransaction inMemOutboxTransaction)
            {
                ICompletableSynchronizedStorageSession session = new FakeSynchronizedStorageSession(inMemOutboxTransaction.Transaction);
                return Task.FromResult(session);
            }
            return EmptyTask;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transportTransaction.TryGet(out Transaction ambientTransaction))
            {
                var transaction = new FakeTransaction();
                ICompletableSynchronizedStorageSession session = new FakeSynchronizedStorageSession(transaction);
                ambientTransaction.EnlistVolatile(new EnlistmentNotification(transaction), EnlistmentOptions.None);
                return Task.FromResult(session);
            }
            return EmptyTask;
        }

        static readonly Task<ICompletableSynchronizedStorageSession> EmptyTask = Task.FromResult<ICompletableSynchronizedStorageSession>(null);

        class EnlistmentNotification : IEnlistmentNotification
        {
            public EnlistmentNotification(FakeTransaction transaction)
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

            FakeTransaction transaction;
        }
    }
}