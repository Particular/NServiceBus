using System.Transactions;
using Microsoft.Samples.ServiceHosting.StorageClient;

namespace NServiceBus.Unicast.Queueing.Azure
{
    public class SendResourceManager : IEnlistmentNotification
    {
        private readonly MessageQueue queue;
        private readonly Message message;

        public SendResourceManager(MessageQueue queue, Message message)
        {
            this.queue = queue;
            this.message = message;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Done();
        }

        public void Commit(Enlistment enlistment)
        {
            queue.PutMessage(message);
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