namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using System;
    using System.Threading;
    using System.Transactions;

    using Apache.NMS;
    using Apache.NMS.Util;

    using NServiceBus.Unicast.Transport.Transactional;

    public class ActiveMqMessageReceiver : INotifyMessageReceived, IProcessMessages
    {
        private readonly IActiveMqPurger purger;
        private readonly IMessageCounter pendingMessagesCounter;
        private readonly IConsumeEvents eventConsumer;
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
            IActiveMqPurger purger,
            IMessageCounter pendingMessagesCounter,
            IConsumeEvents eventConsumer)
        {
            this.sessionFactory = sessionFactory;
            this.activeMqMessageMapper = activeMqMessageMapper;
            this.purger = purger;
            this.pendingMessagesCounter = pendingMessagesCounter;
            this.eventConsumer = eventConsumer;
        }

        ~ActiveMqMessageReceiver()
        {
            this.Disposing(true);
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

            this.PurgeIfNecessary(this.session, destination);

            this.defaultConsumer = this.session.CreateConsumer(destination);
            this.defaultConsumer.Listener += this.OnMessageReceived;

            if (address == Address.Local)
            {
                this.eventConsumer.Start(this.session, this);
            }
        }

        public void Stop()
        {
            this.stop = true;
            Thread.MemoryBarrier(); // Full fence to prevent writing of stop and reading of pending message count to be reordered. 

            this.eventConsumer.Stop();
            this.defaultConsumer.Listener -= this.OnMessageReceived;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Disposing(true);
        }

        private void Disposing(bool disposing)
        {
            this.eventConsumer.Dispose();
            this.defaultConsumer.Close();
            this.defaultConsumer.Dispose();
            this.sessionFactory.Release(this.session);
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

        public void ProcessMessage(IMessage message)
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
                    this.ProcessWithoutTransaction(transportMessage, message);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                this.EndProcessMessage(transportMessage != null ? transportMessage.Id : null, exception);
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
            if (this.PurgeOnStartup)
            {
                this.purger.Purge(session, destination);
            }
        }
    }
}