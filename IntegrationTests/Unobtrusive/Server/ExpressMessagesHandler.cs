namespace Server
{
    using System;
    using Messages;
    using NServiceBus;

    public class ExpressMessagesHandler : IHandleMessages<RequestExpress>
    {
        public void Handle(RequestExpress message)
        {
            Console.Out.WriteLine("Message [{0}] received, id: [{1}]", message.GetType(), message.RequestId);
        }
    }
}