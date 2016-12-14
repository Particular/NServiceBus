namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

        public IEnumerable<IContainSagaData> Sagas(ReadOnlyContextBag context)
        {
            return sagaPersister.Sagas((ContextBag)context);
        }

        public void Enlist(Action action)
        {
            Transaction.Enlist(action);
        }

        readonly InMemorySagaPersister sagaPersister;

        bool ownsTransaction;
    }
}