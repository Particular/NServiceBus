namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Transport;

    /// <summary>
    /// session
    /// </summary>
    public interface IInMemoryStorageSession
    {
        /// <summary>
        /// Finds a specified saga
        /// </summary>
        /// <returns></returns>
        TSagaData FirstOrDefault<TSagaData>(ReadOnlyContextBag context, Func<TSagaData, bool> predicate)
            where TSagaData : IContainSagaData;
    }

    /// <summary>
    /// </summary>
    public static class InMemorySynchronizedStorageSessionExtensions
    {
        /// <summary>
        /// Gets the current context InMemory <see cref="IInMemoryStorageSession" />.
        /// </summary>
        public static IInMemoryStorageSession Session(this SynchronizedStorageSession session)
        {
            var inMemoryStorageSession = (IInMemoryStorageSession) session;
            return inMemoryStorageSession;
        }
    }

    class InMemoryTransactionalSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public InMemoryTransactionalSynchronizedStorageAdapter() : this(null)
        {
        }

        public InMemoryTransactionalSynchronizedStorageAdapter(InMemorySagaPersister sagaPersister)
        {
            inMemorySagaPersister = sagaPersister;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            var inMemOutboxTransaction = transaction as InMemoryOutboxTransaction;
            if (inMemOutboxTransaction != null)
            {
                CompletableSynchronizedStorageSession session = new InMemorySynchronizedStorageSession(inMemOutboxTransaction.Transaction, inMemorySagaPersister);
                return Task.FromResult(session);
            }
            return EmptyTask;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            Transaction ambientTransaction;

            if (transportTransaction.TryGet(out ambientTransaction))
            {
                var transaction = new InMemoryTransaction();
                CompletableSynchronizedStorageSession session = new InMemorySynchronizedStorageSession(transaction, inMemorySagaPersister);
                ambientTransaction.EnlistVolatile(new EnlistmentNotification(transaction), EnlistmentOptions.None);
                return Task.FromResult(session);
            }
            return EmptyTask;
        }

        InMemorySagaPersister inMemorySagaPersister;

        static readonly Task<CompletableSynchronizedStorageSession> EmptyTask = Task.FromResult<CompletableSynchronizedStorageSession>(null);

        class EnlistmentNotification : IEnlistmentNotification
        {
            public EnlistmentNotification(InMemoryTransaction transaction)
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

            InMemoryTransaction transaction;
        }
    }
}