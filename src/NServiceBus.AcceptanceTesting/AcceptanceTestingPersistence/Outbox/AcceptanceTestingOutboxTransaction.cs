namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.Outbox
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Outbox;

    [SkipWeaving]
    public class AcceptanceTestingOutboxTransaction : OutboxTransaction
    {
        public AcceptanceTestingOutboxTransaction()
        {
            Transaction = new AcceptanceTestingTransaction();
        }

        public AcceptanceTestingTransaction Transaction { get; private set; }

        public void Dispose()
        {
            Transaction = null;
        }

        public Task Commit()
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