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
                return this.consumerName;
            }
            set
            {
                this.consumerName = value;
                this.internalConsumerName = value.Replace('.', '-');
            }
        }

        public void Start()
        {
            this.SubscribeTopics();
        }

        public void Stop()
        {
            this.notifyTopicSubscriptions.Unregister(this);
            foreach (var messageConsumer in this.topicConsumers)
            {
                messageConsumer.Value.Listener -= this.messageProcessor.ProcessMessage;
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
            if (this.topicConsumers.TryGetValue(e.Topic, out consumer))
            {
                consumer.Dispose();
                this.topicConsumers.Remove(e.Topic);
            }
        }

        public void TopicSubscribed(object sender, SubscriptionEventArgs e)
        {
            string topic = e.Topic;
            this.Subscribe(topic);
        }

        private void SubscribeTopics()
        {
            lock (this.notifyTopicSubscriptions)
            {
                foreach (string topic in this.notifyTopicSubscriptions.Register(this))
                {
                    this.Subscribe(topic);
                }
            }
        }

        private void Subscribe(string topic)
        {
            var consumer = this.messageProcessor.CreateMessageConsumer(string.Format("queue://Consumer.{0}.{1}", this.internalConsumerName, topic));
            consumer.Listener += this.messageProcessor.ProcessMessage;
            this.topicConsumers[topic] = consumer;
        }
    }
}