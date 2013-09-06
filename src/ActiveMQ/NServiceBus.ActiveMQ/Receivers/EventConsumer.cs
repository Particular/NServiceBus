namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System;
    using System.Collections.Generic;
    using Apache.NMS;

    public class EventConsumer : ITopicSubscriptionListener, IConsumeEvents
    {
        private readonly IDictionary<string, IMessageConsumer> topicConsumers = new Dictionary<string, IMessageConsumer>();
        private readonly INotifyTopicSubscriptions notifyTopicSubscriptions;
        private readonly IProcessMessages messageProcessor;
        private bool disposed;
        private string consumerName;
        private string internalConsumerName;

        public EventConsumer(INotifyTopicSubscriptions notifyTopicSubscriptions, IProcessMessages messageProcessor)
        {
            this.notifyTopicSubscriptions = notifyTopicSubscriptions;
            this.messageProcessor = messageProcessor;
        }

        public string ConsumerName
        {
            get
            {
                return consumerName;
            }
            set
            {
                consumerName = value;
                internalConsumerName = value.Replace('.', '-');
            }
        }

        public void Start()
        {
            SubscribeTopics();
        }

        public void Stop()
        {
            notifyTopicSubscriptions.Unregister(this);
            foreach (var messageConsumer in topicConsumers)
            {
                messageConsumer.Value.Listener -= messageProcessor.ProcessMessage;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            foreach (var messageConsumer in topicConsumers)
            {
                messageConsumer.Value.Dispose();
            }

            disposed = true;
        }

        public void TopicUnsubscribed(object sender, SubscriptionEventArgs e)
        {
            IMessageConsumer consumer;
            if (topicConsumers.TryGetValue(e.Topic, out consumer))
            {
                consumer.Dispose();
                topicConsumers.Remove(e.Topic);
            }
        }

        public void TopicSubscribed(object sender, SubscriptionEventArgs e)
        {
            string topic = e.Topic;
            Subscribe(topic);
        }

        private void SubscribeTopics()
        {
            lock (notifyTopicSubscriptions)
            {
                foreach (string topic in notifyTopicSubscriptions.Register(this))
                {
                    Subscribe(topic);
                }
            }
        }

        private void Subscribe(string topic)
        {
            var consumer = messageProcessor.CreateMessageConsumer(string.Format("queue://Consumer.{0}.{1}", internalConsumerName, topic));
            consumer.Listener += messageProcessor.ProcessMessage;
            topicConsumers[topic] = consumer;
        }
    }
}