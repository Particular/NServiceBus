namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System.Transactions;
    using Microsoft.ServiceBus.Messaging;

    public class ReceiveResourceManager : IEnlistmentNotification
    {
        private readonly BrokeredMessage receivedMessage;

        public ReceiveResourceManager(BrokeredMessage receivedMessage)
        {
            this.receivedMessage = receivedMessage;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            receivedMessage.SafeComplete();
           
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            receivedMessage.SafeAbandon();

            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }


    }
}