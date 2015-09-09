namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;

    abstract class ReceiveStrategy
    {
        public abstract void ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, Action<PushContext> onMessage);
    }
}