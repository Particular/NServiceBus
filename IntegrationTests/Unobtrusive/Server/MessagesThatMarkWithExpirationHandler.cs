namespace Server
{
    using System;
    using Messages;
    using NServiceBus;

    public class MessagesThatMarkWithExpirationHandler : IHandleMessages<MessageThatExpires>
    {
        public void Handle(MessageThatExpires message)
        {
            Console.Out.WriteLine("Message [{0}] received, id: [{1}]", message.GetType(), message.RequestId);
        }
    }
}