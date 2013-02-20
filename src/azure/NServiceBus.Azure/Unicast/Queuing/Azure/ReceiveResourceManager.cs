namespace NServiceBus.Unicast.Queuing.Azure
{
    using System.Transactions;
    using Microsoft.WindowsAzure.StorageClient;

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
            catch (StorageClientException ex)
            {
                if (ex.ErrorCode != StorageErrorCode.ResourceNotFound) throw;
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