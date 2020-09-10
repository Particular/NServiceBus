namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;
    using Persistence;

    [SkipWeaving]
    class InMemorySynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public InMemorySynchronizedStorageSession(InMemoryTransaction transaction)
        {
            Transaction = transaction;
        }

        public InMemorySynchronizedStorageSession()
            : this(new InMemoryTransaction())
        {
            ownsTransaction = true;
        }

        public InMemoryTransaction Transaction { get; private set; }

        public void Dispose()
        {
            Transaction = null;
        }

        public Task CompleteAsync(CancellationToken cancellationToken)
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
    }
}