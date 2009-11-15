using System.Transactions;
using Microsoft.Samples.ServiceHosting.StorageClient;

namespace NServiceBus.Unicast.Queueing.Azure
{
    public class ReceiveResourceManager : IEnlistmentNotification
    {
        private readonly MessageQueue queue;
        private readonly Message receivedMessage;

        public ReceiveResourceManager(MessageQueue queue, Message receivedMessage)
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