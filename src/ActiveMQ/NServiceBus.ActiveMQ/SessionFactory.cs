namespace NServiceBus.Transport.ActiveMQ
{
    using System.Collections.Concurrent;
    using System.Threading;
    using Apache.NMS;

    public class SessionFactory : ISessionFactory
    {
        private readonly INetTxConnectionFactory connectionFactroy;
        
        private readonly ConcurrentBag<INetTxSession> sessionPool = new ConcurrentBag<INetTxSession>();
        private readonly ConcurrentDictionary<INetTxSession, INetTxConnection> connections = new ConcurrentDictionary<INetTxSession, INetTxConnection>();
        private readonly ConcurrentDictionary<int, INetTxSession> sessions = new ConcurrentDictionary<int, INetTxSession>();

        public SessionFactory(INetTxConnectionFactory connectionFactroy)
        {
            this.connectionFactroy = connectionFactroy;
        }

        public INetTxSession GetSession()
        {
            INetTxSession session;
            if (this.sessions.TryGetValue(Thread.CurrentThread.ManagedThreadId, out session))
            {
                return session;
            }

            // Currently in case of DTC the consumer and produce of messages use an own session due to a bug in the ActiveMQ NMS client:
            // https://issues.apache.org/jira/browse/AMQNET-405 . When this issue is resolved then we should return the same session within
            // a DTC transaction to be able to use Single Phase Commits in case no other systems are involved in the transaction for better
            // performance.
            return this.GetSessionFromPool();
        }

        public INetTxSession GetOwnSession()
        {
            return this.GetSessionFromPool();
        }

        public void Release(INetTxSession session)
        {
            if (!this.sessions.ContainsKey(Thread.CurrentThread.ManagedThreadId))
            {
                this.sessionPool.Add(session);
            }
        }

        public void SetSessionForCurrentThread(INetTxSession session)
        {
            this.sessions.AddOrUpdate(Thread.CurrentThread.ManagedThreadId, session, (key, value)  => session);
        }

        public void RemoveSessionForCurrentThread()
        {
            INetTxSession session;
            this.sessions.TryRemove(Thread.CurrentThread.ManagedThreadId, out session);
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
            var connection = this.connectionFactroy.CreateNetTxConnection();
            connection.Start();

            var session = connection.CreateNetTxSession();
            this.connections.TryAdd(session, connection);

            return session;
        }
    }
}