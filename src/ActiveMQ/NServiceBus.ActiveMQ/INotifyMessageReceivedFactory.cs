namespace NServiceBus.Transport.ActiveMQ
{
    using NServiceBus.Transport.ActiveMQ.Receivers;

    public interface INotifyMessageReceivedFactory
    {
        INotifyMessageReceived CreateMessageReceiver();
    }
}