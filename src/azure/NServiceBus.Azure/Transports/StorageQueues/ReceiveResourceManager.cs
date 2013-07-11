namespace NServiceBus.Unicast.Queuing.Azure
{
    using System.Transactions;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    
    public class ReceiveResourceManager : IEnlistmentNotification
    {
        private readonly CloudQueue queue;
        private readonly CloudQueueMessage receivedMessage;

        public ReceiveResourceManager(CloudQueue queue, CloudQueueMessage receivedMessage)
        {
            this.queue = queue;
            this.receivedMessage = receivedMessage;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            try
            {
                queue.DeleteMessage(receivedMessage);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode != 404) throw;
            }
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