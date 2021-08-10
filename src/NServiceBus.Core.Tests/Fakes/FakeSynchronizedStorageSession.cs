namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Persistence;

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

        public void Dispose()
        {
            Transaction = null;
        }

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
    }
}