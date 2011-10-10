namespace MasterNode
{
    using System;
    using MyMessages;
    using NServiceBus;

    public class MessageToBeDistributedHander : IHandleMessages<MessageToBeDistributed>
    {
        public void Handle(MessageToBeDistributed message)
        {
            Console.WriteLine("Message from distributor processed by the locla work in the masternode");
        }
    }
}