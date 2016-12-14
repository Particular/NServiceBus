namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Janitor;
    using Persistence;

    [SkipWeaving]
    class InMemorySynchronizedStorageSession : CompletableSynchronizedStorageSession, IInMemoryStorageSession
    {
        public InMemorySynchronizedStorageSession(InMemoryTransaction transaction, InMemorySagaPersister sagaPersister)
        {
            this.sagaPersister = sagaPersister;
            Transaction = transaction;
        }

        public InMemorySynchronizedStorageSession(InMemorySagaPersister sagaPersister)
            : this(new InMemoryTransaction(), sagaPersister)
        {
            ownsTransaction = true;
        }

        public InMemorySynchronizedStorageSession()
            : this(new InMemoryTransaction(), null)
        {
            ownsTransaction = true;
        }

        public InMemoryTransaction Transaction { get; private set; }

        public void Dispose()
        {
            Transaction = null;
        }

        public Task CompleteAsync()
        {
            if (ownsTransaction)
            {
                Transaction.Commit();
            }
            return TaskEx.CompletedTask;
        }

        public TSagaData FirstOrDefault<TSagaData>(ReadOnlyContextBag context, Func<TSagaData, bool> predicate) 
            where TSagaData : IContainSagaData
        {
            return sagaPersister.FirstOrDefault(context, predicate);
        }

        public void Enlist(Action action)
        {
            Transaction.Enlist(action);
        }

        readonly InMemorySagaPersister sagaPersister;
        
        bool ownsTransaction;
    }
}