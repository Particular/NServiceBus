namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactonsScopes
{
    using System;
    using Apache.NMS;
    using SessionFactories;

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources.
                if (doRollback)
                {
                    session.Rollback();
                }

                sessionFactory.RemoveSessionForCurrentThread();
            }

            disposed = true;
        }

        ~ActiveMqTransaction()
        {
            Dispose(false);
        }
    }
}