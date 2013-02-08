namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using System;
    using System.Threading;

    using Apache.NMS;
    using Apache.NMS.Util;

    using NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes;
    using NServiceBus.Unicast.Transport.Transactional;

    public class MessageProcessor : IProcessMessages
    {
        private readonly IMessageCounter pendingMessagesCounter;
        private readonly IActiveMqMessageMapper activeMqMessageMapper;
        private readonly IActiveMqPurger purger;
        private readonly ISessionFactory sessionFactory;
        private readonly ITransactionScopeFactory transactionScopeFactory;

        private volatile bool stop = false;
        public ISession session;
        private TransactionSettings transactionSettings;

        public Action<string, Exception> EndProcessMessage { get; set; }
        public Func<TransportMessage, bool> TryProcessMessage { get; set; }
        public bool PurgeOnStartup { get; set; }

        public MessageProcessor(
            IMessageCounter pendingMessagesCounter,
            IActiveMqMessageMapper activeMqMessageMapper,
            ISessionFactory sessionFactory,
            IActiveMqPurger purger,
            ITransactionScopeFactory transactionScopeFactory)
        {
            this.pendingMessagesCounter = pendingMessagesCounter;
            this.activeMqMessageMapper = activeMqMessageMapper;
            this.sessionFactory = sessionFactory;
            this.purger = purger;
            this.transactionScopeFactory = transactionScopeFactory;
        }

        public virtual void Start(TransactionSettings transactionSettings)
        {
            this.transactionSettings = transactionSettings;
            this.session = this.sessionFactory.GetSession();
        }

        public void Stop()
        {
            this.stop = true;
            Thread.MemoryBarrier(); // Full fence to prevent writing of stop and reading of pending message count to be reordered. 
        }

        public void Dispose()
        {
            this.sessionFactory.Release(this.session);
        }
        
        public void ProcessMessage(IMessage message)
        {
            try
            {
                this.pendingMessagesCounter.Increment();

                Thread.MemoryBarrier(); // Full fence to prevent writing pending message count and reading of stop to be reordered. 

                using (var tx = transactionScopeFactory.CreateNewTransactionScope(this.transactionSettings, this.session))
                {
                    if (this.stop)
                    {
                        return;
                    }

                    tx.MessageAccepted(message);
                    TransportMessage transportMessage = null;
                    Exception exception = null;

                    try
                    {
                        transportMessage = this.activeMqMessageMapper.CreateTransportMessage(message);
                        if (TryProcessMessage(transportMessage))
                        {
                            tx.Complete();
                        }
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                    finally
                    {
                        EndProcessMessage(transportMessage != null ? transportMessage.Id : null, exception);
                    }
                }
            }
            finally
            {
                this.pendingMessagesCounter.Decrement();
            }
        }

        public IMessageConsumer CreateMessageConsumer(string destination)
        {
            IDestination d = SessionUtil.GetDestination(this.session, destination);
            this.PurgeIfNecessary(this.session, d);
            return this.session.CreateConsumer(d);
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