namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes
{
    using System;

    using Apache.NMS;

    using NServiceBus.Transports.ActiveMQ.SessionFactories;

    public class ActiveMqTransaction : ITransactionScope
    {
        private readonly ISessionFactory sessionFactory;
        private readonly ISession session;

        bool disposed;
        private bool doRollback = true;

        public ActiveMqTransaction(ISessionFactory sessionFactory, ISession session)
        {
            this.sessionFactory = sessionFactory;
            this.session = session;

            this.sessionFactory.SetSessionForCurrentThread(this.session);
        }

        public void MessageAccepted(IMessage message)
        {
        }

        public void Complete()
        {
            session.Commit();
            doRollback = false;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            // Dispose managed resources.
            if (doRollback)
            {
                session.Rollback();
            }

            sessionFactory.RemoveSessionForCurrentThread();
            disposed = true;
        }
    }
}