using System;
using System.Threading;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
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
            try
            {
                sender.Send(message);
            }
            catch (ServerBusyException)
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                sender.Send(message);
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