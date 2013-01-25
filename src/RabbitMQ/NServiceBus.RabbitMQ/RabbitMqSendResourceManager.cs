namespace NServiceBus.RabbitMq
{
    using System;
    using System.Transactions;

  
    public class RabbitMqSendResourceManager : IEnlistmentNotification
    {
        readonly Action action;

        public RabbitMqSendResourceManager(Action action)
        {
            this.action = action;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            action();
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