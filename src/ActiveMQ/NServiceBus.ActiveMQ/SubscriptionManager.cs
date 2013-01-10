namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Collections.Generic;

    using NServiceBus.Unicast.Subscriptions;

    public class SubscriptionManager : INotifyTopicSubscriptions, IManageSubscriptions
    {
        private readonly ISet<string> subscriptions = new HashSet<string>();
        private readonly ITopicEvaluator topicEvaluator;

        private event EventHandler<SubscriptionEventArgs> TopicSubscribed = delegate { };
        private event EventHandler<SubscriptionEventArgs> TopicUnsubscribed = delegate { };

        public SubscriptionManager(ITopicEvaluator topicEvaluator)
        {
            this.topicEvaluator = topicEvaluator;
        }

        public IEnumerable<string> Register(ITopicSubscriptionListener listener)
        {
            lock (subscriptions)
            {
                this.TopicSubscribed += listener.TopicSubscribed;
                this.TopicUnsubscribed += listener.TopicUnsubscribed;
                return new HashSet<string>(this.subscriptions);
            }
        }

        public void Unregister(ITopicSubscriptionListener listener)
        {
            lock (subscriptions)
            {
                this.TopicSubscribed -= listener.TopicSubscribed;
                this.TopicUnsubscribed -= listener.TopicUnsubscribed;
            }
        }

        public void Subscribe(Type eventType, Address publisherAddress, Predicate<object> condition)
        {
            var topic = this.topicEvaluator.GetTopicFromMessageType(eventType);

            lock (subscriptions)
            {
                if (this.subscriptions.Add(topic))
                {
                    this.TopicSubscribed(this, new SubscriptionEventArgs(topic));
                }
            }
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            var topic = this.topicEvaluator.GetTopicFromMessageType(eventType);

            lock (subscriptions)
            {
                if (this.subscriptions.Remove(topic))
                {
                    this.TopicUnsubscribed(this, new SubscriptionEventArgs(topic));
                }
            }
        }

        public event EventHandler<Unicast.SubscriptionEventArgs> ClientSubscribed;
    }
}