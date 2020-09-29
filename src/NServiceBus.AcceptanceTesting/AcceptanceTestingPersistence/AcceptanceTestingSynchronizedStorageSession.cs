namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using Persistence;

    [SkipWeaving]
    class AcceptanceTestingSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public AcceptanceTestingSynchronizedStorageSession(AcceptanceTestingTransaction transaction)
        {
            Transaction = transaction;
        }

        public AcceptanceTestingSynchronizedStorageSession()
            : this(new AcceptanceTestingTransaction())
        {
            ownsTransaction = true;
        }

        public AcceptanceTestingTransaction Transaction { get; private set; }

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
            return Task.CompletedTask;
        }

        public void Enlist(Action action)
        {
            Transaction.Enlist(action);
        }

        bool ownsTransaction;
    }
}