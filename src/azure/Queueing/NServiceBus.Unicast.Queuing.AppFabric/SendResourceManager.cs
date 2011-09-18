using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.AppFabric
{
    public class SendResourceManager : IEnlistmentNotification
    {
        private readonly QueueClient sender;
        private readonly BrokeredMessage message;

        public SendResourceManager(QueueClient sender, BrokeredMessage message)
        {
            this.sender = sender;
            this.message = message;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            sender.Send(message);
            
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