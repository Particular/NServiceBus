namespace Server
{
    using System;
    using Messages;
    using NServiceBus;

    public class LargeMessagesHandler : IHandleMessages<LargeMessage>
    {
        public void Handle(LargeMessage message)
        {
            if(message.LargeDataBus != null)
                Console.Out.WriteLine("Message [{0}] received, id:{1} and payload {2} bytes", message.GetType(), message.RequestId, message.LargeDataBus.Length);
            else
            {
                Console.Out.WriteLine("Message [{0}] received, id:{1}", message.GetType(), message.RequestId);
            }
        }
    }
}