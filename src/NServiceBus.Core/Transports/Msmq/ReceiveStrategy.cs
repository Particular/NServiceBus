namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;
    using System.Threading.Tasks;

    abstract class ReceiveStrategy
    {
        public abstract Task ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, Func<PushContext, Task> onMessage);
    }
}