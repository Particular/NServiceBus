namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System;
    using System.Threading;
    using System.Transactions;
    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using Apache.NMS.Util;

    using TransactionsScopes;

    using SessionFactories;

    using Unicast.Transport;

    public class MessageProcessor : IProcessMessages
    {
        private readonly IActiveMqMessageMapper activeMqMessageMapper;
        private readonly IActiveMqPurger purger;
        private readonly ISessionFactory sessionFactory;
        private readonly ITransactionScopeFactory transactionScopeFactory;
        readonly AutoResetEvent resetEvent = new AutoResetEvent(true);

        private volatile bool stop;
        private ISession session;
        private TransactionSettings transactionSettings;
        private bool disposed;

        public Action<TransportMessage, Exception> EndProcessMessage { get; set; }
        public Func<TransportMessage, bool> TryProcessMessage { get; set; }
        public bool PurgeOnStartup { get; set; }


        public MessageProcessor(
            IActiveMqMessageMapper activeMqMessageMapper,
            ISessionFactory sessionFactory,
            IActiveMqPurger purger,
            ITransactionScopeFactory transactionScopeFactory)
        {
            this.activeMqMessageMapper = activeMqMessageMapper;
            this.sessionFactory = sessionFactory;
            this.purger = purger;
            this.transactionScopeFactory = transactionScopeFactory;
        }

        public virtual void Start(TransactionSettings transactionSettings)
        {
            this.transactionSettings = transactionSettings;
            session = sessionFactory.GetSession();
        }

        public void Stop()
        {
            stop = true;
            resetEvent.WaitOne();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (sessionFactory != null)
            {
                sessionFactory.Release(session);
            }

            disposed = true;
        }

        public void ProcessMessage(IMessage message)
        {
            if (stop)
            {
                return;
            }

            try
            {
                resetEvent.Reset();

                if (stop)
                {
                    resetEvent.Set();
                    return;
                }

                using (var tx = transactionScopeFactory.CreateNewTransactionScope(transactionSettings, session))
                {
                    tx.MessageAccepted(message);
                    TransportMessage transportMessage = null;
                    Exception exception = null;

                    try
                    {
                        transportMessage = activeMqMessageMapper.CreateTransportMessage(message);
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
                        EndProcessMessage(transportMessage, exception);
                    }
                }
            }
            finally
            {
                resetEvent.Set();
            }
        }

        public IMessageConsumer CreateMessageConsumer(string destination)
        {
            var d = SessionUtil.GetDestination(session, destination);
            PurgeIfNecessary(session, d);
            var consumer = session.CreateConsumer(d);
            ((MessageConsumer)consumer).CreateTransactionScopeForAsyncMessage = CreateTransactionScopeForAsyncMessage;
            return consumer;
        }

        private TransactionScope CreateTransactionScopeForAsyncMessage()
        {
            return transactionScopeFactory.CreateTransactionScopeForAsyncMessage(transactionSettings);
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