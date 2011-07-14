using System;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.AppFabric
{
    public class SendResourceManager : IEnlistmentNotification
    {
        private readonly MessageSender sender;
        private readonly BrokeredMessage message;

        public SendResourceManager(MessageSender sender, BrokeredMessage message)
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
            catch (Exception)
            {
                
                throw;
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