namespace NServiceBus.ActiveMQ
{
    using System;
    using System.Collections.Concurrent;
    using System.Transactions;

    using Apache.NMS;

    public class SessionFactory : ISessionFactory
    {
        private readonly INetTxConnectionFactory connectionFactroy;
        
        private readonly ConcurrentBag<INetTxSession> sessionPool = new ConcurrentBag<INetTxSession>();
        private readonly ConcurrentDictionary<INetTxSession, INetTxConnection> connections = new ConcurrentDictionary<INetTxSession, INetTxConnection>();
        private readonly ConcurrentDictionary<string, INetTxSession> sessions = new ConcurrentDictionary<string, INetTxSession>();

        public SessionFactory(INetTxConnectionFactory connectionFactroy)
        {
            this.connectionFactroy = connectionFactroy;
        }

        public INetTxSession GetSession()
        {
            return InTransaction() ? this.GetSessionForTransaction() : this.GetSessionFromPool();
        }

        public void Release(INetTxSession session)
        {
            if (!InTransaction())
            {
                this.sessionPool.Add(session);
            }
        }

        public void SetSessionForCurrentTransaction(INetTxSession session)
        {
            this.sessions.AddOrUpdate(Transaction.Current.TransactionInformation.LocalIdentifier, session, (key, value)  => session);
        }

        public void RemoveSessionForCurrentTransaction()
        {
            INetTxSession session;
            this.sessions.TryRemove(Transaction.Current.TransactionInformation.LocalIdentifier, out session);
        }

        private static bool InTransaction()
        {
            return Transaction.Current != null;
        }

        private INetTxSession GetSessionForTransaction()
        {
            return this.sessions.GetOrAdd(
                Transaction.Current.TransactionInformation.LocalIdentifier,
                key =>
                {
                    var session = this.GetSessionFromPool();
                    this.RegisterRemoveSessionOnTransactionComplete();
                    return session;
                });
        }

        private void RegisterRemoveSessionOnTransactionComplete()
        {
            Transaction.Current.TransactionCompleted += (s, e) =>
            {
                try
                {
                    INetTxSession session;
                    this.sessions.TryRemove(e.Transaction.TransactionInformation.LocalIdentifier, out session);
                    this.sessionPool.Add(session);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            };
        }

        private INetTxSession GetSessionFromPool()
        {
            INetTxSession session;
            if (this.sessionPool.TryTake(out session))
            {
                return session;
            }

            return this.CreateNewSession();
        }

        private INetTxSession CreateNewSession()
        {
            var connection = this.connectionFactroy.CreateNetTxConnection();
            connection.Start();

            var session = connection.CreateNetTxSession();
            this.connections.TryAdd(session, connection);

            return session;
        }
    }
}