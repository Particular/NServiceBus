namespace NServiceBus.Transports.ActiveMQ
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
                TopicSubscribed += listener.TopicSubscribed;
                TopicUnsubscribed += listener.TopicUnsubscribed;
                return new HashSet<string>(subscriptions);
            }
        }

        public void Unregister(ITopicSubscriptionListener listener)
        {
            lock (subscriptions)
            {
                TopicSubscribed -= listener.TopicSubscribed;
                TopicUnsubscribed -= listener.TopicUnsubscribed;
            }
        }

        public void Subscribe(Type eventType, Address publisherAddress)
        {
            var topic = topicEvaluator.GetTopicFromMessageType(eventType);

            lock (subscriptions)
            {
                if (subscriptions.Add(topic))
                {
                    TopicSubscribed(this, new SubscriptionEventArgs(topic));
                }
            }
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            var topic = topicEvaluator.GetTopicFromMessageType(eventType);

            lock (subscriptions)
            {
                if (subscriptions.Remove(topic))
                {
                    TopicUnsubscribed(this, new SubscriptionEventArgs(topic));
                }
            }
        }
    }
}