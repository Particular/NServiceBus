namespace NServiceBus.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using Apache.NMS;
    using Apache.NMS.Util;
    using Unicast.Transport;

    public class ActiveMqMessageReceiver : INotifyMessageReceived
    {
        private readonly IActiveMqMessageMapper activeMqMessageMapper;
        private readonly INetTxConnection connection;
        private readonly IActiveMqPurger purger;
        private readonly ISubscriptionManager subscriptionManager;

        private readonly IDictionary<string, IMessageConsumer> topicConsumers =
            new Dictionary<string, IMessageConsumer>();

        private IMessageConsumer defaultConsumer;
        private ISession session;

        public ActiveMqMessageReceiver(INetTxConnection connection, IActiveMqMessageMapper activeMqMessageMapper,
                                       ISubscriptionManager subscriptionManager, IActiveMqPurger purger)
        {
            this.connection = connection;
            this.activeMqMessageMapper = activeMqMessageMapper;
            this.subscriptionManager = subscriptionManager;
            this.purger = purger;
        }

        public string ConsumerName { get; set; }

        /// <summary>
        ///     Sets whether or not the transport should purge the input
        ///     queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        public event EventHandler<TransportMessageReceivedEventArgs> MessageReceived = delegate { };

        public void Start(Address address)
        {
            session = connection.CreateNetTxSession();
            IDestination destination = SessionUtil.GetDestination(session, "queue://" + address.Queue);

            PurgeIfNecessary(session, destination);

            defaultConsumer = session.CreateConsumer(destination);
            defaultConsumer.Listener += OnMessageReceived;

            if (address == Address.Local)
            {
                SubscribeTopics();
            }
        }

        public void Dispose()
        {
            foreach (var messageConsumer in topicConsumers)
            {
                messageConsumer.Value.Close();
                messageConsumer.Value.Dispose();
            }

            defaultConsumer.Close();
            defaultConsumer.Dispose();
            session.Close();
            session.Dispose();
        }

        private void OnTopicUnsubscribed(object sender, SubscriptionEventArgs e)
        {
            IMessageConsumer consumer;
            if (topicConsumers.TryGetValue(e.Topic, out consumer))
            {
                consumer.Dispose();
                topicConsumers.Remove(e.Topic);
            }
        }

        private void OnTopicSubscribed(object sender, SubscriptionEventArgs e)
        {
            string topic = e.Topic;
            Subscribe(topic);
        }

        private void SubscribeTopics()
        {
            lock (subscriptionManager)
            {
                subscriptionManager.TopicSubscribed += OnTopicSubscribed;
                subscriptionManager.TopicUnsubscribed += OnTopicUnsubscribed;

                foreach (string topic in subscriptionManager.GetTopics())
                {
                    Subscribe(topic);
                }
            }
        }

        private void Subscribe(string topic)
        {
            IDestination destination = SessionUtil.GetDestination(session,
                                                                  string.Format("queue://Consumer.{0}.{1}", ConsumerName,
                                                                                topic));
            PurgeIfNecessary(session, destination);

            IMessageConsumer consumer = session.CreateConsumer(destination);
            consumer.Listener += OnMessageReceived;
            topicConsumers[topic] = consumer;
        }

        private void OnMessageReceived(IMessage message)
        {
            TransportMessage transportMessage = activeMqMessageMapper.CreateTransportMessage(message);
            MessageReceived(this, new TransportMessageReceivedEventArgs(transportMessage));
        }

        private void PurgeIfNecessary(ISession session, IDestination destination)
        {
            if (PurgeOnStartup)
            {
                purger.Purge(session, destination);
            }
        }
    }
}