namespace NServiceBus.Transports.ActiveMQ.SessionFactories
{
    using System;
    using System.Collections.Concurrent;
    using Apache.NMS;

    public class PooledSessionFactory : ISessionFactory
    {
        private readonly IConnectionFactory connectionFactory;
        private readonly ConcurrentBag<ISession> sessionPool = new ConcurrentBag<ISession>();
        private readonly ConcurrentDictionary<ISession, IConnection> connections = new ConcurrentDictionary<ISession, IConnection>();
        private bool disposed;

        public PooledSessionFactory(IConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public virtual ISession GetSession()
        {
            ISession session;
            if (sessionPool.TryTake(out session))
            {
                return session;
            }

            return CreateNewSession();
        }

        public virtual void Release(ISession session)
        {
            sessionPool.Add(session);
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
            var connection = connectionFactory.CreateConnection();
            connection.Start();

            var session = connection.CreateSession();
            connections.TryAdd(session, connection);

            return session;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            foreach (var connection in connections)
            {
                connection.Value.Close();
            }

            disposed = true;
        }
    }
}