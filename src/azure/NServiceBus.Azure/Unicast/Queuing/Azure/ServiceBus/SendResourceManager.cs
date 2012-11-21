namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using System.Transactions;

    public class SendResourceManager : IEnlistmentNotification
    {
        private readonly Action onCommit;

        public SendResourceManager(Action onCommit )
        {
            this.onCommit = onCommit;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            onCommit();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
    }
}