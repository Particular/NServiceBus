namespace NServiceBus.Transports.ActiveMQ.SessionFactories
{
    using System;
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
            if (this.sessionsForThreads.TryGetValue(Thread.CurrentThread.ManagedThreadId, out session))
            {
                return session;
            }

            return this.pooledSessionFactory.GetSession();
        }

        public void Release(ISession session)
        {
            if (this.sessionsForThreads.ContainsKey(Thread.CurrentThread.ManagedThreadId))
            {
                return;
            }

            session.Commit();
            this.pooledSessionFactory.Release(session);
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