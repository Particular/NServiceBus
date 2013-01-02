namespace NServiceBus.Transport.ActiveMQ
{
    public class NotifyMessageReceivedFactory : INotifyMessageReceivedFactory
    {
        public INotifyMessageReceived CreateMessageReceiver()
        {
            return Configure.Instance.Builder.Build<INotifyMessageReceived>();
        }
    }
}