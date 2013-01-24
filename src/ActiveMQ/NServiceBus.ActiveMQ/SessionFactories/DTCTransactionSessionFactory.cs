﻿namespace NServiceBus.Transport.ActiveMQ.SessionFactories
{
    using System;
    using System.Collections.Concurrent;
    using System.Transactions;

    using Apache.NMS;

    public class DTCTransactionSessionFactory : ISessionFactory
    {
        private readonly ISessionFactory pooledSessionFactory;
        private readonly ConcurrentDictionary<string, ISession> sessionsForTransactions = new ConcurrentDictionary<string, ISession>();

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
                return this.sessionsForTransactions.GetOrAdd(
                    Transaction.Current.TransactionInformation.LocalIdentifier, id => this.GetSessionForTransaction());
            }

            return this.pooledSessionFactory.GetSession();
        }

        public void Release(ISession session)
        {
            if (Transaction.Current != null)
            {
                return;
            }

            this.pooledSessionFactory.Release(session);
        }

        private ISession GetSessionForTransaction()
        {
            var session = this.pooledSessionFactory.GetSession();

            Transaction.Current.TransactionCompleted += (s, e) => this.ReleaseSessionForTransaction(e.Transaction);

            return session;
        }

        private void ReleaseSessionForTransaction(Transaction transaction)
        {
            ISession session;
            this.sessionsForTransactions.TryRemove(transaction.TransactionInformation.LocalIdentifier, out session);
            this.pooledSessionFactory.Release(session);
        }

        public virtual void SetSessionForCurrentThread(ISession session)
        {
            throw new NotSupportedException("Thread specific sessions are not supported by this implementation.");
        }

        public virtual void RemoveSessionForCurrentThread()
        {
            throw new NotSupportedException("Thread specific sessions are not supported by this implementation.");
        }
    }
}