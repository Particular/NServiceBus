namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    public interface INotifyMessageReceivedFactory
    {
        INotifyMessageReceived CreateMessageReceiver();
    }
}