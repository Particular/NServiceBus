namespace NServiceBus.ActiveMQ
{
    using System.Collections.Concurrent;
    using System.Threading;

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
            return this.sessions.GetOrAdd(Thread.CurrentThread.ManagedThreadId, key => this.connection.CreateNetTxSession());
        }
    }
}