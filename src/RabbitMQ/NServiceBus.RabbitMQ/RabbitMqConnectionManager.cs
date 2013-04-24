namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using Config;
    using EasyNetQ;
    using Logging;
    using global::RabbitMQ.Client;

    public class RabbitMqConnectionManager : IDisposable, IManageRabbitMqConnections
    {
        public RabbitMqConnectionManager(IConnectionFactory connectionFactory,IConnectionConfiguration connectionConfiguration)
        {
            this.connectionFactory = connectionFactory;
            this.connectionConfiguration = connectionConfiguration;
        }

        public IConnection GetConnection(ConnectionPurpose purpose)
        {
            //note: The purpose is there so that we/users can add more advanced connection managers in the future

            lock (connectionFactory)
            {
                if (connectionFailed)
                    throw connectionFailedReason;

                return connection ?? (connection = new PersistentConnection(connectionFactory, connectionConfiguration.RetryDelay));
            }
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
                if (connection == null)
                {
                    return;
                }

                connection.Dispose();
                connection = null;
            }

            disposed = true;
        }

        ~RabbitMqConnectionManager()
        {
            Dispose(false);
        }

        readonly IConnectionFactory connectionFactory;
        readonly IConnectionConfiguration connectionConfiguration;
        PersistentConnection connection;
        bool connectionFailed;
        Exception connectionFailedReason;
        bool disposed;

        static readonly ILog Logger = LogManager.GetLogger(typeof(RabbitMqConnectionManager));
    }
}