using System.Transactions;
using Microsoft.WindowsAzure.StorageClient;

namespace NServiceBus.Unicast.Queuing.Azure
{
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

            queue.DeleteMessage(receivedMessage);

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