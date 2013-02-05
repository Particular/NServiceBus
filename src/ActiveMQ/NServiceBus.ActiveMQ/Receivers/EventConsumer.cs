namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using System.Collections.Generic;

    using Apache.NMS;
    using Apache.NMS.Util;

    public class EventConsumer : ITopicSubscriptionListener, IConsumeEvents
    {
        private readonly IActiveMqPurger purger;

        private readonly IDictionary<string, IMessageConsumer> topicConsumers = new Dictionary<string, IMessageConsumer>();
        private readonly INotifyTopicSubscriptions notifyTopicSubscriptions;
        private IProcessMessages messageProcessor;
        private ISession session;

        public EventConsumer(INotifyTopicSubscriptions notifyTopicSubscriptions, IActiveMqPurger purger)
        {
            this.notifyTopicSubscriptions = notifyTopicSubscriptions;
            this.purger = purger;
        }

        public string ConsumerName { get; set; }

        /// <summary>
        ///     Sets whether or not the transport should purge the input
        ///     queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        public void Start(ISession session, IProcessMessages messageProcessor)
        {
            this.messageProcessor = messageProcessor;
            this.session = session;
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
            foreach (var messageConsumer in this.topicConsumers)
            {
                messageConsumer.Value.Close();
                messageConsumer.Value.Dispose();
            }
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
            IDestination destination = SessionUtil.GetDestination(this.session,
                                                                  string.Format("queue://Consumer.{0}.{1}", this.ConsumerName, topic));
            this.PurgeIfNecessary(this.session, destination);

            IMessageConsumer consumer = this.session.CreateConsumer(destination);
            consumer.Listener += this.messageProcessor.ProcessMessage;
            this.topicConsumers[topic] = consumer;
        }

        private void PurgeIfNecessary(ISession session, IDestination destination)
        {
            if (this.PurgeOnStartup)
            {
                this.purger.Purge(session, destination);
            }
        }
    }
}