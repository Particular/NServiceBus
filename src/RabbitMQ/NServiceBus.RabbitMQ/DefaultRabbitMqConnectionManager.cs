namespace NServiceBus.RabbitMq
{
    using System;
    using System.Threading;
    using Logging;
    using global::RabbitMQ.Client;

    public class DefaultRabbitMqConnectionManager : IDisposable, IManageRabbitMqConnections
    {
        public int MaxRetries { get; set; }

        public TimeSpan DelayBetweenRetries { get; set; }

        public DefaultRabbitMqConnectionManager(ConnectionFactory connectionFactory)
        {
            MaxRetries = 6;
            DelayBetweenRetries = TimeSpan.FromSeconds(10);

            this.connectionFactory = connectionFactory;
        }

        public IConnection GetConnection(ConnectionPurpose purpose,string callerId)
        {
            //note: The purpose+callerId is there so that we/users can add more advanced connection managers in the future

            lock (connectionFactory)
            {
                if (connectionFailed)
                    throw connectionFailedReason;

                return connection ?? (connection = TryCreateConnection());
            }
        }

        IConnection TryCreateConnection()
        {
            int retries = 0;
            Exception exception = null;
            while (retries < MaxRetries)
            {
                try
                {
                    var connection = connectionFactory.CreateConnection();

                    connection.ConnectionShutdown += ConnectionOnConnectionShutdown;

                    return connection;
                }
                catch (Exception ex)
                {
                    connectionFailedReason = ex;
                    retries++;

                    Logger.Warn("Failed to create a RabbitMq connection", ex);
                }

                Thread.Sleep(DelayBetweenRetries);
            }
            connectionFailed = true;

            Configure.Instance.RaiseCriticalError("Failed to connect to the RabbitMq broker.", exception);

            throw exception;

        }

        void ConnectionOnConnectionShutdown(IConnection connection, ShutdownEventArgs reason)
        {
            Logger.ErrorFormat("The connection the the RabbitMq broker was closed, reason: {0}",reason);
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

                if (connection.IsOpen)
                {
                    connection.Close();
                }

                connection.Dispose();
                connection = null;
            }

            disposed = true;
        }

        ~DefaultRabbitMqConnectionManager()
        {
            Dispose(false);
        }

        readonly ConnectionFactory connectionFactory;
        IConnection connection;
        static readonly ILog Logger = LogManager.GetLogger("RabbitMq");
        bool connectionFailed;
        Exception connectionFailedReason;
        bool disposed;
    }
}