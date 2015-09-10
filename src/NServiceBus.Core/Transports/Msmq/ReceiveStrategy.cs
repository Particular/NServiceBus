namespace NServiceBus
{
    using System;
    using System.Messaging;
    using System.Threading.Tasks;
    using NServiceBus.Transports;

    abstract class ReceiveStrategy
    {
        public abstract Task ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, Func<PushContext, Task> onMessage);
    }
}