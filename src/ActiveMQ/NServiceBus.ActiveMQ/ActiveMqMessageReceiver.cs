namespace NServiceBus.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;

    using Apache.NMS;
    using Apache.NMS.Util;

    using NServiceBus.Unicast.Transport;
    using NServiceBus.Utils;

    public class ActiveMqMessageReceiver : INotifyMessageReceived
    {
        private readonly ISubscriptionManager subscriptionManager;

        private readonly IActiveMqPurger purger;

        private readonly IDictionary<string, IMessageConsumer> topicConsumers = new Dictionary<string, IMessageConsumer>();
        private readonly ISessionFactory sessionFactory;
        private readonly IActiveMqMessageMapper activeMqMessageMapper;

        private INetTxSession session;
        private IMessageConsumer defaultConsumer;

        public event EventHandler<TransportMessageReceivedEventArgs> MessageReceived = delegate { };

        public ActiveMqMessageReceiver(
            ISessionFactory sessionFactory, 
            IActiveMqMessageMapper activeMqMessageMapper, 
            ISubscriptionManager subscriptionManager, 
            IActiveMqPurger purger)
        {
            this.sessionFactory = sessionFactory;
            this.activeMqMessageMapper = activeMqMessageMapper;
            this.subscriptionManager = subscriptionManager;
            this.purger = purger;
        }

        public string ConsumerName { get; set; }

        /// <summary>
        /// Sets whether or not the transport should purge the input
        /// queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        public void Start(Address address)
        {
            this.session = this.sessionFactory.CreateSession();
            IDestination destination = SessionUtil.GetDestination(this.session, "queue://" + address.Queue);

            this.PurgeIfNecessary(this.session, destination);

            this.defaultConsumer = this.session.CreateConsumer(destination);
            this.defaultConsumer.Listener += this.OnMessageReceived;

            if (address == Address.Local)
            {
                this.SubscribeTopics();
            }
        }

        private void OnTopicUnsubscribed(object sender, SubscriptionEventArgs e)
        {
            IMessageConsumer consumer;
            if (this.topicConsumers.TryGetValue(e.Topic, out consumer))
            {
                consumer.Dispose();
                this.topicConsumers.Remove(e.Topic);
            }
        }

        private void OnTopicSubscribed(object sender, SubscriptionEventArgs e)
        {
            var topic = e.Topic;
            this.Subscribe(topic);
        }

        private void SubscribeTopics()
        {
            lock (this.subscriptionManager)
            {
                this.subscriptionManager.TopicSubscribed += this.OnTopicSubscribed;
                this.subscriptionManager.TopicUnsubscribed += this.OnTopicUnsubscribed;

                foreach (var topic in this.subscriptionManager.GetTopics())
                {
                    this.Subscribe(topic);
                }
            }
        }

        private void Subscribe(string topic)
        {
            var destination = SessionUtil.GetDestination(this.session, string.Format("queue://Consumer.{0}.{1}", this.ConsumerName, topic));
            this.PurgeIfNecessary(this.session, destination);

            var consumer = this.session.CreateConsumer(destination);
            consumer.Listener += this.OnMessageReceived;
            this.topicConsumers[topic] = consumer;
        }

        private void OnMessageReceived(IMessage message)
        {
            var transportMessage = this.activeMqMessageMapper.CreateTransportMessage(message);
            this.MessageReceived(this, new TransportMessageReceivedEventArgs(transportMessage));
        }

        private void PurgeIfNecessary(ISession session, IDestination destination)
        {
            if (this.PurgeOnStartup)
            {
                this.purger.Purge(session, destination);
            }
        }

        public void Dispose()
        {
            foreach (var messageConsumer in this.topicConsumers)
            {
                messageConsumer.Value.Close();
                messageConsumer.Value.Dispose();
            }

            this.defaultConsumer.Close();
            this.defaultConsumer.Dispose();

            this.sessionFactory.Release(this.session);
        }
    }
}