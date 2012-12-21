namespace NServiceBus.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;

    using Apache.NMS;
    using Apache.NMS.Util;

    using NServiceBus.Unicast.Transport.Transactional;

    public class ActiveMqMessageReceiver : INotifyMessageReceived
    {
        private readonly IActiveMqPurger purger;
        private readonly ISubscriptionManager subscriptionManager;

        private readonly IDictionary<string, IMessageConsumer> topicConsumers = 
            new Dictionary<string, IMessageConsumer>();
        private readonly ISessionFactory sessionFactory;
        private readonly IActiveMqMessageMapper activeMqMessageMapper;

        private INetTxSession session;
        private IMessageConsumer defaultConsumer;
        private TransactionSettings transactionSettings;
        private TransactionOptions transactionOptions;

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
        ///     Sets whether or not the transport should purge the input
        ///     queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        public Func<TransportMessage, bool> TryProcessMessage { get; set; }

        public void Start(Address address, TransactionSettings transactionSettings)
        {
            this.transactionSettings = transactionSettings;
            this.transactionOptions = new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout };

            this.session = this.sessionFactory.GetSession();
            IDestination destination = SessionUtil.GetDestination(this.session, "queue://" + address.Queue);

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
            foreach (var messageConsumer in this.topicConsumers)
            {
                messageConsumer.Value.Close();
                messageConsumer.Value.Dispose();
            }

            this.defaultConsumer.Close();
            this.defaultConsumer.Dispose();

            this.sessionFactory.Release(this.session);
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
            var transportMessage = this.activeMqMessageMapper.CreateTransportMessage(message);

            if (this.transactionSettings.IsTransactional)
            {
                if (!this.transactionSettings.SuppressDTC)
                {
                    this.ProcessInDTCTransaction(transportMessage);
                }
                else
                {
                    this.ProcessInActiveMqTransaction(transportMessage);
                }
            }
            else
            {
                this.TryProcessMessage(transportMessage);
            }
        }

        private void ProcessInActiveMqTransaction(TransportMessage transportMessage)
        {
            if (!this.TryProcessMessage(transportMessage))
            {
                this.session.Rollback();
            }
        }

        private void ProcessInDTCTransaction(TransportMessage transportMessage)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, this.transactionOptions))
            {
                this.sessionFactory.SetSessionForCurrentTransaction(this.session);
                var success = this.TryProcessMessage(transportMessage);
                this.sessionFactory.RemoveSessionForCurrentTransaction();

                if (success)
                {
                    scope.Complete();
                }
            }
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