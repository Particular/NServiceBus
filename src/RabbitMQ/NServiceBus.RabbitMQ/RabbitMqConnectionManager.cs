namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using Config;
    using EasyNetQ;
    using global::RabbitMQ.Client;

    public class RabbitMqConnectionManager : IDisposable, IManageRabbitMqConnections
    {
        public RabbitMqConnectionManager(IConnectionFactory connectionFactory, IConnectionConfiguration connectionConfiguration)
        {
            this.connectionFactory = connectionFactory;
            this.connectionConfiguration = connectionConfiguration;
        }

        public IConnection GetConnection(ConnectionPurpose purpose)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            //note: The purpose is there so that we/users can add more advanced connection managers in the future
            lock (connectionFactory)
            {
                return connection ?? (connection = new PersistentConnection(connectionFactory, connectionConfiguration.RetryDelay));
            }
        }

        public void Dispose()
        {
            disposed = true;

            if (connection == null)
            {
                return;
            }

            // Dispose managed resources.
            connection.Dispose();
        }

        readonly IConnectionFactory connectionFactory;
        readonly IConnectionConfiguration connectionConfiguration;
        PersistentConnection connection;
        bool disposed;
    }
}