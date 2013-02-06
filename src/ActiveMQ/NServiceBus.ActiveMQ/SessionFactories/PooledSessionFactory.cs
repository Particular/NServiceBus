namespace NServiceBus.Transport.ActiveMQ.SessionFactories
{
    using System;
    using System.Collections.Concurrent;

    using Apache.NMS;

    public class PooledSessionFactory : ISessionFactory
    {
        private readonly IConnectionFactory connectionFactory;
        private readonly ConcurrentBag<ISession> sessionPool = new ConcurrentBag<ISession>();
        private readonly ConcurrentDictionary<ISession, IConnection> connections = new ConcurrentDictionary<ISession, IConnection>();

        public PooledSessionFactory(IConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public virtual ISession GetSession()
        {
            ISession session;
            if (this.sessionPool.TryTake(out session))
            {
                return session;
            }

            return this.CreateNewSession();
        }

        public virtual void Release(ISession session)
        {
            this.sessionPool.Add(session);
        }

        public virtual void SetSessionForCurrentThread(ISession session)
        {
            throw new NotSupportedException("Thread specific sessions are not supported by this implementation.");
        }

        public virtual void RemoveSessionForCurrentThread()
        {
            throw new NotSupportedException("Thread specific sessions are not supported by this implementation.");
        }

        protected ISession CreateNewSession()
        {
            var connection = this.connectionFactory.CreateConnection();
            connection.Start();

            var session = connection.CreateSession();
            this.connections.TryAdd(session, connection);

            return session;
        }

        public void Dispose()
        {
            foreach (var connection in connections)
            {
                connection.Value.Close();
            }
        }
    }
}