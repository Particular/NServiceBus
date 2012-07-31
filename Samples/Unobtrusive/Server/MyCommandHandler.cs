using Messages;

namespace Server
{
    using System;
    using Commands;
    using NServiceBus;

    public class MyCommandHandler : IHandleMessages<MyCommand>
    {
        readonly IBus bus;

        public MyCommandHandler(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(MyCommand message)
        {
            Console.WriteLine("Command received, id:" + message.CommandId);
            Console.WriteLine("EncryptedString:" + message.EncryptedString);

            bus.Return(CommandStatus.Ok);
        }
    }
    public class LargeMessagesHandler : IHandleMessages<LargeMessage>
    {
        public void Handle(LargeMessage message)
        {
            if(message.LargeDataBus != null)
                Console.Out.WriteLine("Message received, id:" + message.RequestId + "and payload " + message.LargeDataBus.Length);
            else
            {
                Console.Out.WriteLine("Message received, id:" + message.RequestId + "and payload is null");
            }
        }
    }
    public class MessagesThatMarkWithExpirationHandler : IHandleMessages<MessageThatExpires>
    {
        public void Handle(MessageThatExpires message)
        {
            Console.Out.WriteLine("Message [{0}] received, id: [{1}]", message.GetType(), message.RequestId);
        }
    }
    public class ExpressMessagesHandler : IHandleMessages<RequestExpress>
    {
        public void Handle(RequestExpress message)
        {
            Console.Out.WriteLine("Message [{0}] received, id: [{1}]", message.GetType(), message.RequestId);
        }
    }
}