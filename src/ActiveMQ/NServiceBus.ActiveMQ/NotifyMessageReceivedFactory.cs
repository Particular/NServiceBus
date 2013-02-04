namespace NServiceBus.Transport.ActiveMQ
{
    using NServiceBus.Transport.ActiveMQ.Receivers;

    public class NotifyMessageReceivedFactory : INotifyMessageReceivedFactory
    {
        public INotifyMessageReceived CreateMessageReceiver()
        {
            return Configure.Instance.Builder.Build<INotifyMessageReceived>();
        }
    }
}