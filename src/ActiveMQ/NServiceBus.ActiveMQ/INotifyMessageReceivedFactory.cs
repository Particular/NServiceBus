namespace NServiceBus.Transport.ActiveMQ
{
    public interface INotifyMessageReceivedFactory
    {
        INotifyMessageReceived CreateMessageReceiver();
    }
}