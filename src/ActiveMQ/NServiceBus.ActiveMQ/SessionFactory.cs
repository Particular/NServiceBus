namespace NServiceBus.ActiveMQ
{
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Transactions;

    using Apache.NMS;

    public class SessionFactory : ISessionFactory
    {
        private readonly INetTxConnection connection;
        private readonly ConcurrentDictionary<int, INetTxSession> sessions = 
            new ConcurrentDictionary<int, INetTxSession>();

        public SessionFactory(INetTxConnection connection)
        {
            this.connection = connection;
        }

        public INetTxSession CreateSession()
        {
            return this.connection.CreateNetTxSession();
        }

        public void Release(INetTxSession session)
        {
            if (Transaction.Current != null)
            {
                Transaction.Current.TransactionCompleted += (s, e) => session.Dispose();
            }
            else
            {
                session.Dispose();
            }
        }
    }
}