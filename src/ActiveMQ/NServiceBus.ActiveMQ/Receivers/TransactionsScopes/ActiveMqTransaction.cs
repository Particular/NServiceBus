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
            this.session.Commit();
            this.doRollback = false;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources.
                if (this.doRollback)
                {
                    this.session.Rollback();
                }

                this.sessionFactory.RemoveSessionForCurrentThread();
            }

            this.disposed = true;
        }

        ~ActiveMqTransaction()
        {
            this.Dispose(false);
        }
    }
}