using NServiceBus;
using NServiceBus.Unicast.Queuing.ActiveMQ;

public interface INotifyMessageReceivedFactory
{
    INotifyMessageReceived CreateMessageReceiver();
}

public class NotifyMessageReceivedFactory : INotifyMessageReceivedFactory
{
    public INotifyMessageReceived CreateMessageReceiver()
    {
        return Configure.Instance.Builder.Build<INotifyMessageReceived>();
    }
}