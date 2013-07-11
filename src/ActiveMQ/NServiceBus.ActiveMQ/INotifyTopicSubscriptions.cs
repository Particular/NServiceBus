namespace NServiceBus.Transports.ActiveMQ
{
    using System.Collections.Generic;

    public interface INotifyTopicSubscriptions
    {
        IEnumerable<string> Register(ITopicSubscriptionListener listener);
        void Unregister(ITopicSubscriptionListener listener);
    }
}