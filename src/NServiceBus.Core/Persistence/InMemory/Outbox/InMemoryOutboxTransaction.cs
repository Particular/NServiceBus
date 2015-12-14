namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Outbox;

    [SkipWeaving]
    class InMemoryOutboxTransaction : OutboxTransaction
    {
        public InMemoryTransaction Transaction { get; private set; }

        public InMemoryOutboxTransaction()
        {
            Transaction = new InMemoryTransaction();
        }
         
        public void Enlist(Action action)
        {
            Transaction.Enlist(action);
        }

        public void Dispose()
        {
            Transaction = null;
        }

        public Task Commit()
        {
            Transaction.Commit();
            return TaskEx.Completed;
        }
    }
}