﻿namespace NServiceBus.ActiveMQ
{
    using System.Collections.Generic;

    using Apache.NMS;

    using Moq;

    using NServiceBus.Transport.ActiveMQ;

    internal class PooledSessionFactoryMock : ISessionFactory
    {
        public readonly Queue<ISession> sessions = new Queue<ISession>();

        public List<ISession> EnqueueNewSessions(int count)
        {
            var result = new List<ISession>();

            for (int i = 0; i < count; i++ )
            {
                var session = new Mock<ISession>().Object;
                this.sessions.Enqueue(session);
                result.Add(session);
            }

            return result;
        }

        public ISession GetSession()
        {
            return this.sessions.Dequeue();
        }

        public void Release(ISession session)
        {
            this.sessions.Enqueue(session);
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