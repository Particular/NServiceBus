namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes
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
            if (disposed)
            {
                return;
            }

            if (sessionFactory != null)
            {
                sessionFactory.RemoveSessionForCurrentThread();
            }
            if (transactionScope != null)
            {
                transactionScope.Dispose();
            }
            if (!complete)
            {
                throw new Exception();
            }

            disposed = true;
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