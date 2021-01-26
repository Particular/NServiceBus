namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Outbox;

    class AcceptanceTestingOutboxTransaction : OutboxTransaction
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