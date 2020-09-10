namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;
    using Outbox;

    [SkipWeaving]
    class InMemoryOutboxTransaction : OutboxTransaction
    {
        public InMemoryOutboxTransaction()
        {
            Transaction = new InMemoryTransaction();
        }

        public InMemoryTransaction Transaction { get; private set; }

        public void Dispose()
        {
            Transaction = null;
        }

        public Task Commit(CancellationToken cancellationToken)
        {
            Transaction.Commit();
            return Task.CompletedTask;
        }

        public void Enlist(Action action)
        {
            Transaction.Enlist(action);
        }
    }
}