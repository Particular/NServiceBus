namespace NServiceBus.Unicast.Queuing.Azure
{
    using System.Transactions;
    using Microsoft.WindowsAzure.Storage.Queue;
    
    public class SendResourceManager : IEnlistmentNotification
    {
        private readonly CloudQueue queue;
        private readonly CloudQueueMessage message;

        public SendResourceManager(CloudQueue queue, CloudQueueMessage message)
        {
            this.queue = queue;
            this.message = message;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            queue.AddMessage(message);
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