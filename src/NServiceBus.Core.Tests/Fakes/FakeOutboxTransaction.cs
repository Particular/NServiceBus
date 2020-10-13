namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using System.Threading.Tasks;
    using Outbox;

    public class FakeOutboxTransaction : OutboxTransaction
    {
        public FakeOutboxTransaction()
        {
            Transaction = new FakeTransaction();
        }

        public FakeTransaction Transaction { get; private set; }

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