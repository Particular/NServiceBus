namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactonsScopes
{
    using System;
    using System.Transactions;
    using Apache.NMS;
    using SessionFactories;

    public class DTCTransactionScope : ITransactionScope
    {
        private readonly ISessionFactory sessionFactory;

        private readonly TransactionScope transactionScope;
        private bool complete;
        bool disposed;

        public DTCTransactionScope(ISession session, TransactionOptions transactionOptions, ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
            transactionScope = new TransactionScope(TransactionScopeOption.Required, transactionOptions);
            this.sessionFactory.SetSessionForCurrentThread(session);
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
                this.sessionFactory.RemoveSessionForCurrentThread();
                transactionScope.Dispose();
                if (!complete)
                {
                    throw new Exception();
                }
            }

            disposed = true;
        }

        ~DTCTransactionScope()
        {
            Dispose(false);
        }

        public void MessageAccepted(IMessage message)
        {
        }

        public void Complete()
        {
            complete = true;
            transactionScope.Complete();
        }
    }
}