namespace NServiceBus.ActiveMQ
{
    public interface INotifyMessageReceivedFactory
    {
        INotifyMessageReceived CreateMessageReceiver();
    }
}