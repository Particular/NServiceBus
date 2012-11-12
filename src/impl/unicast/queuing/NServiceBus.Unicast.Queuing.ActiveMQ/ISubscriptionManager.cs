namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;
    using System.Collections.Generic;

    public interface ISubscriptionManager
    {
        event EventHandler<SubscriptionEventArgs> TopicSubscribed;
        event EventHandler<SubscriptionEventArgs> TopicUnsubscribed;

        IEnumerable<string> GetTopics();
        void Subscribe(string topic);
        void Unsubscribe(string topic);
    }
}