namespace NServiceBus
{
    using System;
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

        public Task Commit()
        {
            Transaction.Commit();
            return TaskEx.CompletedTask;
        }

        public void Enlist(Action action)
        {
            Transaction.Enlist(action);
        }
    }
}