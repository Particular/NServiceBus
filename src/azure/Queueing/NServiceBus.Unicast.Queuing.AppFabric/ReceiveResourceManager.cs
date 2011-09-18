using System;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.AppFabric
{
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
            try
            {
                receivedMessage.Complete();
            }
            catch (MessageLockLostException)
            {
                // message has been completed by another thread or worker
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