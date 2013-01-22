namespace NServiceBus.Transport.ActiveMQ
{
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Transactions;

    using Apache.NMS;

    public class SessionFactory : ISessionFactory
    {
        private readonly INetTxConnectionFactory connectionFactory;

        private readonly ConcurrentBag<INetTxSession> sessionPool = new ConcurrentBag<INetTxSession>();
        private readonly ConcurrentDictionary<INetTxSession, INetTxConnection> connections = new ConcurrentDictionary<INetTxSession, INetTxConnection>();
        private readonly ConcurrentDictionary<int, INetTxSession> sessionsForThreads = new ConcurrentDictionary<int, INetTxSession>();
        private readonly ConcurrentDictionary<string, INetTxSession> sessionsForTransactions = new ConcurrentDictionary<string, INetTxSession>();

        public SessionFactory(INetTxConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public INetTxSession GetSession()
        {
            INetTxSession session;
            if (this.sessionsForThreads.TryGetValue(Thread.CurrentThread.ManagedThreadId, out session))
            {
                return session;
            }

            if (Transaction.Current != null)
            {
                // Currently in case of DTC the consumer and produce of messages use an own session due to a bug in the ActiveMQ NMS client:
                // https://issues.apache.org/jira/browse/AMQNET-405 . When this issue is resolved then we should return the same session within
                // a DTC transaction to be able to use Single Phase Commits in case no other systems are involved in the transaction for better
                // performance.
                return this.sessionsForTransactions.GetOrAdd(
                    Transaction.Current.TransactionInformation.LocalIdentifier, id => this.GetSessionForTransaction());
            }

            return this.GetSessionFromPool();
        }

        public INetTxSession GetOwnSession()
        {
            return this.GetSessionFromPool();
        }

        public void Release(INetTxSession session)
        {
            if (this.sessionsForThreads.ContainsKey(Thread.CurrentThread.ManagedThreadId))
            {
                return;
            }

            if (Transaction.Current != null)
            {
                return;
            }

            this.sessionPool.Add(session);
        }

        public void SetSessionForCurrentThread(INetTxSession session)
        {
            this.sessionsForThreads.AddOrUpdate(Thread.CurrentThread.ManagedThreadId, session, (key, value)  => session);
        }

        public void RemoveSessionForCurrentThread()
        {
            INetTxSession session;
            this.sessionsForThreads.TryRemove(Thread.CurrentThread.ManagedThreadId, out session);
        }

        private INetTxSession GetSessionForTransaction()
        {
            var session = this.GetSessionFromPool();

            Transaction.Current.TransactionCompleted += (s, e) => this.ReleaseSessionForTransaction(e.Transaction);
            
            return session;
        }

        private void ReleaseSessionForTransaction(Transaction transaction)
        {
            INetTxSession session;
            this.sessionsForTransactions.TryRemove(transaction.TransactionInformation.LocalIdentifier, out session);
            this.sessionPool.Add(session);
        }

        private INetTxSession GetSessionFromPool()
        {
            INetTxSession session;
            if (this.sessionPool.TryTake(out session))
            {
                return session;
            }

            return this.CreateNewSession();
        }

        private INetTxSession CreateNewSession()
        {
            var connection = this.connectionFactory.CreateNetTxConnection();
            connection.Start();

            var session = connection.CreateNetTxSession();
            this.connections.TryAdd(session, connection);

            return session;
        }
    }
}