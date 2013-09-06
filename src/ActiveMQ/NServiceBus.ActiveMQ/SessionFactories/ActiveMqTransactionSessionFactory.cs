namespace NServiceBus.Transports.ActiveMQ.SessionFactories
{
    using System.Collections.Concurrent;
    using System.Threading;
    using Apache.NMS;

    public class ActiveMqTransactionSessionFactory : ISessionFactory
    {
        private readonly ISessionFactory pooledSessionFactory;
        private readonly ConcurrentDictionary<int, ISession> sessionsForThreads = new ConcurrentDictionary<int, ISession>();
        bool disposed;

        public ActiveMqTransactionSessionFactory(ISessionFactory pooledSessionFactory)
        {
            this.pooledSessionFactory = pooledSessionFactory;
        }

        public ISession GetSession()
        {
            ISession session;
            if (sessionsForThreads.TryGetValue(Thread.CurrentThread.ManagedThreadId, out session))
            {
                return session;
            }

            return pooledSessionFactory.GetSession();
        }

        public void Release(ISession session)
        {
            if (sessionsForThreads.ContainsKey(Thread.CurrentThread.ManagedThreadId))
            {
                return;
            }

            session.Commit();
            pooledSessionFactory.Release(session);
        }

        public void SetSessionForCurrentThread(ISession session)
        {
            sessionsForThreads.AddOrUpdate(Thread.CurrentThread.ManagedThreadId, session, (key, value)  => session);
        }

        public void RemoveSessionForCurrentThread()
        {
            ISession session;
            sessionsForThreads.TryRemove(Thread.CurrentThread.ManagedThreadId, out session);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (pooledSessionFactory != null)
            {
                pooledSessionFactory.Dispose();
            }

            disposed = true;
        }
    }
}