namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Persistence;

    [SkipWeaving]
    class InMemorySynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        bool ownsTransaction;

        public InMemoryTransaction Transaction { get; private set; }

        public InMemorySynchronizedStorageSession(InMemoryTransaction transaction)
        {
            Transaction = transaction;
        }

        public InMemorySynchronizedStorageSession()
            : this(new InMemoryTransaction())
        {
            ownsTransaction = true;
        }

        public void Enlist(Action action)
        {
            Transaction.Enlist(action);
        }

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
    }
}