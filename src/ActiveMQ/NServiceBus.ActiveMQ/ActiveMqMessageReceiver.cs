namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Transactions;

    using Apache.NMS;
    using Apache.NMS.Util;
    using Logging;
    using NServiceBus.Unicast.Transport.Transactional;

    public class ActiveMqMessageReceiver : INotifyMessageReceived, ITopicSubscriptionListener
    {
        private readonly IActiveMqPurger purger;
        private readonly IMessageCounter pendingMessagesCounter;
        private readonly INotifyTopicSubscriptions notifyTopicSubscriptions;

        private readonly IDictionary<string, IMessageConsumer> topicConsumers = 
            new Dictionary<string, IMessageConsumer>();
        private readonly ISessionFactory sessionFactory;
        private readonly IActiveMqMessageMapper activeMqMessageMapper;

        private ISession session;
        private IMessageConsumer defaultConsumer;
        private TransactionSettings transactionSettings;
        private TransactionOptions transactionOptions;
        private volatile bool stop = false;

        public ActiveMqMessageReceiver(
            ISessionFactory sessionFactory, 
            IActiveMqMessageMapper activeMqMessageMapper, 
            INotifyTopicSubscriptions notifyTopicSubscriptions, 
            IActiveMqPurger purger,
            IMessageCounter pendingMessagesCounter)
        {
            this.sessionFactory = sessionFactory;
            this.activeMqMessageMapper = activeMqMessageMapper;
            this.notifyTopicSubscriptions = notifyTopicSubscriptions;
            this.purger = purger;
            this.pendingMessagesCounter = pendingMessagesCounter;
        }


        public string ConsumerName { get; set; }

        /// <summary>
        ///     Sets whether or not the transport should purge the input
        ///     queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        public Action<string, Exception> EndProcessMessage { get; set; }
        public Func<TransportMessage, bool> TryProcessMessage { get; set; }

        public void Start(Address address, TransactionSettings transactionSettings)
        {
            this.transactionSettings = transactionSettings;
            this.transactionOptions = new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout };

            this.session = this.sessionFactory.GetSession();
            var destination = SessionUtil.GetDestination(this.session, "queue://" + address.Queue);

            PurgeIfNecessary(session, destination);

            defaultConsumer = session.CreateConsumer(destination);
            defaultConsumer.Listener += OnMessageReceived;

            if (address == Address.Local)
            {
                SubscribeTopics();
            }
        }

        public void Stop()
        {
            this.stop = true;
            Thread.MemoryBarrier(); // Full fence to prevent writing of stop and reading of pending message count to be reordered. 

            this.notifyTopicSubscriptions.Unregister(this);
            this.defaultConsumer.Listener -= this.OnMessageReceived;
            foreach (var messageConsumer in this.topicConsumers)
            {
                messageConsumer.Value.Listener -= this.OnMessageReceived;
            }
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
            lock (this.notifyTopicSubscriptions)
            {
                foreach (string topic in this.notifyTopicSubscriptions.Register(this))
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
            try
            {
                this.pendingMessagesCounter.Increment();

                Thread.MemoryBarrier(); // Full fence to prevent writing pending message count and reading of stop to be reordered. 
                this.ProcessMessage(message);
            }
            finally
            {
                this.pendingMessagesCounter.Decrement();
            }
        }

        private void ProcessMessage(IMessage message)
        {
            TransportMessage transportMessage = null;
            Exception exception = null;

            try
            {
                transportMessage = this.activeMqMessageMapper.CreateTransportMessage(message);

                if (this.transactionSettings.IsTransactional)
                {
                    if (!this.transactionSettings.DontUseDistributedTransactions)
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
                    ProcessWithoutTransaction(transportMessage, message);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                EndProcessMessage(transportMessage != null ? transportMessage.Id : null, exception);
            }
        }

        private void ProcessWithoutTransaction(TransportMessage transportMessage, IMessage message)
        {
            if (!this.stop)
            {
                this.TryProcessMessage(transportMessage);
                message.Acknowledge();
            }
        }

        private void ProcessInActiveMqTransaction(TransportMessage transportMessage)
        {
            this.sessionFactory.SetSessionForCurrentThread(this.session);
            var success = !this.stop && this.TryProcessMessage(transportMessage);
            this.sessionFactory.RemoveSessionForCurrentThread();

            if (success)
            {
                this.session.Commit();
            }
            else
            {
                this.session.Rollback();
            }
        }

        private void ProcessInDTCTransaction(TransportMessage transportMessage)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, this.transactionOptions))
            {
                if (!this.stop && this.TryProcessMessage(transportMessage))
                {
                    scope.Complete();
                }
                else
                {
                    throw new Exception("Processing of message failed, throwing exception to force rollback.");
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

        ~ActiveMqMessageReceiver()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                try
                {
                    foreach (var messageConsumer in this.topicConsumers)
                    {
                        messageConsumer.Value.Close();
                        messageConsumer.Value.Dispose();
                    }

                    defaultConsumer.Close();
                    defaultConsumer.Dispose();

                    sessionFactory.Release(session);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to dispose the receiver",ex);
                }
            }

            // release any unmanaged objects
            // set the object references to null

            disposed = true;
        }

        bool disposed;

        static ILog Logger = LogManager.GetLogger("ActiveMq");
    }
}