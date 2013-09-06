namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes
{
    using System;
    using System.Transactions;
    using Apache.NMS;
    using SessionFactories;

    public class DTCTransactionScope : ITransactionScope
    {
        ISessionFactory sessionFactory;
        TransactionScope transactionScope;
        bool complete;

        public DTCTransactionScope(ISession session, TransactionOptions transactionOptions, ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
            transactionScope = new TransactionScope(TransactionScopeOption.Required, transactionOptions);
            this.sessionFactory.SetSessionForCurrentThread(session);
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
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