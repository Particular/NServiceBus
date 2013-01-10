namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Collections.Generic;

    public interface INotifyTopicSubscriptions
    {
        IEnumerable<string> Register(ITopicSubscriptionListener listener);
        void Unregister(ITopicSubscriptionListener listener);
    }
}