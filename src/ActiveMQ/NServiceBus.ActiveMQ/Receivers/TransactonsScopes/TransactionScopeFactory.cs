namespace NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes
{
    using System.Transactions;

    using Apache.NMS;

    using NServiceBus.Unicast.Transport.Transactional;

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
                return new ActiveMqTransaction(this.sessionFactory, session);
            }

            return new DTCTransactionScope(session, new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout });
        }
    }
}