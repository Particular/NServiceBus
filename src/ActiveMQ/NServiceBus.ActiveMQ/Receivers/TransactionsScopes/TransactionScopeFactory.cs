namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes
{
    using System.Transactions;

    using Apache.NMS;

    using NServiceBus.Transports.ActiveMQ.SessionFactories;
    using NServiceBus.Unicast.Transport;

    public class TransactionScopeFactory : ITransactionScopeFactory
    {
        private readonly ISessionFactory sessionFactory;

        public TransactionScopeFactory(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public ITransactionScope CreateNewTransactionScope(TransactionSettings transactionSettings, ISession session)
        {
            if (!transactionSettings.IsTransactional)
            {
                return new NoTransactionScope();
            }

            if (transactionSettings.DontUseDistributedTransactions)
            {
                return new ActiveMqTransaction(sessionFactory, session);
            }

            return new DTCTransactionScope(session, new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout }, sessionFactory);
        }

        public TransactionScope CreateTransactionScopeForAsyncMessage(TransactionSettings transactionSettings)
        {
            if (!transactionSettings.IsTransactional)
            {
                return null;
            }

            if (transactionSettings.DontUseDistributedTransactions)
            {
                return null;
            }

            return new TransactionScope(TransactionScopeOption.RequiresNew, 
                new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout });
        }
    }
}