namespace NServiceBus.Transport.ActiveMQ
{
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Transactions;

    using Apache.NMS;

    public class SessionFactory : ISessionFactory
    {
        private readonly INetTxConnectionFactory connectionFactory;

        private readonly ConcurrentBag<ISession> sessionPool = new ConcurrentBag<ISession>();
        private readonly ConcurrentDictionary<ISession, INetTxConnection> connections = new ConcurrentDictionary<ISession, INetTxConnection>();
        private readonly ConcurrentDictionary<int, ISession> sessionsForThreads = new ConcurrentDictionary<int, ISession>();
        private readonly ConcurrentDictionary<string, ISession> sessionsForTransactions = new ConcurrentDictionary<string, ISession>();

        public SessionFactory(INetTxConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public ISession GetSession()
        {
            ISession session;
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

        public ISession GetOwnSession()
        {
            return this.GetSessionFromPool();
        }

        public void Release(ISession session)
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

        public void SetSessionForCurrentThread(ISession session)
        {
            this.sessionsForThreads.AddOrUpdate(Thread.CurrentThread.ManagedThreadId, session, (key, value)  => session);
        }

        public void RemoveSessionForCurrentThread()
        {
            ISession session;
            this.sessionsForThreads.TryRemove(Thread.CurrentThread.ManagedThreadId, out session);
        }

        private ISession GetSessionForTransaction()
        {
            var session = this.GetSessionFromPool();

            Transaction.Current.TransactionCompleted += (s, e) => this.ReleaseSessionForTransaction(e.Transaction);
            
            return session;
        }

        private void ReleaseSessionForTransaction(Transaction transaction)
        {
            ISession session;
            this.sessionsForTransactions.TryRemove(transaction.TransactionInformation.LocalIdentifier, out session);
            this.sessionPool.Add(session);
        }

        private ISession GetSessionFromPool()
        {
            ISession session;
            if (this.sessionPool.TryTake(out session))
            {
                return session;
            }

            return this.CreateNewSession();
        }

        private ISession CreateNewSession()
        {
            var connection = this.connectionFactory.CreateNetTxConnection();
            connection.Start();

            var session = connection.CreateNetTxSession();
            this.connections.TryAdd(session, connection);

            return session;
        }
    }
}