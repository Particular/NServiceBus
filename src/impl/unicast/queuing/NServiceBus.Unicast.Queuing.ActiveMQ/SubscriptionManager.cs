namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;
    using System.Collections.Generic;

    public class SubscriptionManager : ISubscriptionManager
    {
        readonly ISet<string> subscriptions = new HashSet<string>();

        public event EventHandler<SubscriptionEventArgs> TopicSubscribed;
        public event EventHandler<SubscriptionEventArgs> TopicUnsubscribed;

        public IEnumerable<string> GetTopics()
        {
            return new HashSet<string>(this.subscriptions);
        }

        public void Subscribe(string topic)
        {
            if (this.subscriptions.Add(topic) && this.TopicSubscribed != null)
            {
                this.TopicSubscribed(this, new SubscriptionEventArgs(topic));
            }
        }

        public void Unsubscribe(string topic)
        {
            if (this.subscriptions.Remove(topic) && this.TopicUnsubscribed != null)
            {
                this.TopicUnsubscribed(this, new SubscriptionEventArgs(topic));
            }
        }
    }
}