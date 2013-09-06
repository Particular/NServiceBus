namespace NServiceBus.Transports.ActiveMQ.Tests.SessionFactories
{
    using System.Collections.Generic;
    using Apache.NMS;
    using Moq;
    using NServiceBus.Transports.ActiveMQ.SessionFactories;

    internal class PooledSessionFactoryMock : ISessionFactory
    {
        public readonly Queue<ISession> sessions = new Queue<ISession>();

        public List<ISession> EnqueueNewSessions(int count)
        {
            var result = new List<ISession>();

            for (var i = 0; i < count; i++ )
            {
                var session = new Mock<ISession>().Object;
                sessions.Enqueue(session);
                result.Add(session);
            }

            return result;
        }

        public ISession GetSession()
        {
            return sessions.Dequeue();
        }

        public void Release(ISession session)
        {
            sessions.Enqueue(session);
        }

        public void SetSessionForCurrentThread(ISession session)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveSessionForCurrentThread()
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}