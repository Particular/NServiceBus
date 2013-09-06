namespace NServiceBus.Transports.ActiveMQ.SessionFactories
{
    using System;
    using System.Collections.Concurrent;
    using System.Transactions;
    using Apache.NMS;

    public class DTCTransactionSessionFactory : ISessionFactory
    {
        ISessionFactory pooledSessionFactory;
        ConcurrentDictionary<string, ISession> sessionsForTransactions = new ConcurrentDictionary<string, ISession>();

        public DTCTransactionSessionFactory(ISessionFactory pooledSessionFactory) 
        {
            this.pooledSessionFactory = pooledSessionFactory;
        }

        public ISession GetSession()
        {
            if (Transaction.Current != null)
            {
                // Currently in case of DTC the consumer and produce of messages use an own session due to a bug in the ActiveMQ NMS client:
                // https://issues.apache.org/jira/browse/AMQNET-405 . When this issue is resolved then we should return the same session within
                // a DTC transaction to be able to use Single Phase Commits in case no other systems are involved in the transaction for better
                // performance.
                return sessionsForTransactions.GetOrAdd(
                    Transaction.Current.TransactionInformation.LocalIdentifier, id => GetSessionForTransaction());
            }

            return pooledSessionFactory.GetSession();
        }

        public void Release(ISession session)
        {
            if (Transaction.Current != null)
            {
                return;
            }

            pooledSessionFactory.Release(session);
        }

        ISession GetSessionForTransaction()
        {
            var session = pooledSessionFactory.GetSession();

            Transaction.Current.TransactionCompleted += (s, e) => ReleaseSessionForTransaction(e.Transaction);

            return session;
        }

        void ReleaseSessionForTransaction(Transaction transaction)
        {
            ISession session;
            sessionsForTransactions.TryRemove(transaction.TransactionInformation.LocalIdentifier, out session);
            pooledSessionFactory.Release(session);
        }

        [ThreadStatic]
        static string transactionId;

        public virtual void SetSessionForCurrentThread(ISession session)
        {
            transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
            sessionsForTransactions.TryAdd(transactionId, session);
        }

        public virtual void RemoveSessionForCurrentThread()
        {
            ISession session;
            sessionsForTransactions.TryRemove(transactionId, out session);
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            if (pooledSessionFactory != null)
            {
                pooledSessionFactory.Dispose();
            }
        }
    }
}