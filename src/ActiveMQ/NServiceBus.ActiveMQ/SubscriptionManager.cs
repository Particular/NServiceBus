namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Collections.Generic;

    public class SubscriptionManager : ISubscriptionManager
    {
        readonly ISet<string> subscriptions = new HashSet<string>();

        public event EventHandler<SubscriptionEventArgs> TopicSubscribed = delegate { };
        public event EventHandler<SubscriptionEventArgs> TopicUnsubscribed = delegate { };

        public IEnumerable<string> GetTopics()
        {
            return new HashSet<string>(this.subscriptions);
        }

        public void Subscribe(string topic)
        {
            if (this.subscriptions.Add(topic))
            {
                this.TopicSubscribed(this, new SubscriptionEventArgs(topic));
            }
        }

        public void Unsubscribe(string topic)
        {
            if (this.subscriptions.Remove(topic))
            {
                this.TopicUnsubscribed(this, new SubscriptionEventArgs(topic));
            }
        }
    }
}