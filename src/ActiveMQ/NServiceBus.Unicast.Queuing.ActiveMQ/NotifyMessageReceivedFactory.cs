namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    public class NotifyMessageReceivedFactory : INotifyMessageReceivedFactory
    {
        public INotifyMessageReceived CreateMessageReceiver()
        {
            return Configure.Instance.Builder.Build<INotifyMessageReceived>();
        }
    }
}