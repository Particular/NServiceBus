namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes
{
    using System;
    using System.Transactions;

    using Apache.NMS;

    using NServiceBus.Transports.ActiveMQ.SessionFactories;

    public class DTCTransactionScope : ITransactionScope
    {
        private readonly ISessionFactory sessionFactory;

        private readonly TransactionScope transactionScope;
        private bool complete;
        bool disposed;

        public DTCTransactionScope(ISession session, TransactionOptions transactionOptions, ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
            this.transactionScope = new TransactionScope(TransactionScopeOption.Required, transactionOptions);
            this.sessionFactory.SetSessionForCurrentThread(session);
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
                this.sessionFactory.RemoveSessionForCurrentThread();
                this.transactionScope.Dispose();
                if (!this.complete)
                {
                    throw new Exception();
                }
            }

            this.disposed = true;
        }

        ~DTCTransactionScope()
        {
            this.Dispose(false);
        }

        public void MessageAccepted(IMessage message)
        {
        }

        public void Complete()
        {
            this.complete = true;
            this.transactionScope.Complete();
        }
    }
}