namespace NServiceBus.Transport.ActiveMQ
{
    using System;

    using NServiceBus.Transport.ActiveMQ.Receivers;

    public interface INotifyMessageReceivedFactory
    {
        INotifyMessageReceived CreateMessageReceiver(Func<TransportMessage, bool> tryProcessMessage, Action<string, Exception> endProcessMessage);
    }
}