namespace NServiceBus.RabbitMq
{
    using System;
    using System.Threading;
    using Logging;
    using global::RabbitMQ.Client;

    public class RabbitMqConnectionManager : IDisposable, IManageRabbitMqConnections
    {
        public int MaxRetries { get; set; }

        public TimeSpan DelayBetweenRetries { get; set; }

        public RabbitMqConnectionManager(ConnectionFactory connectionFactory)
        {
            MaxRetries = 6;
            DelayBetweenRetries = TimeSpan.FromSeconds(10);

            this.connectionFactory = connectionFactory;
        }

        public IConnection GetConnection(ConnectionPurpose purpose)
        {
            //note: The purpose is there so that we/users can add more advanced connection managers in the future

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
                    if (retries > 0)
                        Logger.InfoFormat("Issuing retry attempt {0}",retries);

                    
                    var connection = connectionFactory.CreateConnection();

                    connection.ConnectionShutdown += ConnectionOnConnectionShutdown;

                    if(retries > 0)
                        Logger.InfoFormat("Connection to {0} re-established",connectionFactory.HostName);

                    return connection;
                }
                catch (Exception ex)
                {
                    connectionFailedReason = ex;
                    retries++;

                    Logger.Warn("Failed to connect to RabbitMq broker - " + connectionFactory.HostName, ex);
                }

                Thread.Sleep(DelayBetweenRetries);
            }
            connectionFailed = true;

            Configure.Instance.RaiseCriticalError("Failed to connect to the RabbitMq broker.", exception);

            throw exception;

        }

        void ConnectionOnConnectionShutdown(IConnection currentConnection, ShutdownEventArgs reason)
        {
            Logger.WarnFormat("The connection the the RabbitMq broker was closed, reason: {0} , going to reconnect",reason);

            lock (connectionFactory)
            {
                //setting the connection to null will cause the next call to try to create a new connection
                connection = null;
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

                connection.ConnectionShutdown -= ConnectionOnConnectionShutdown;

                if (connection.IsOpen)
                {
                    connection.Close();
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

        readonly ConnectionFactory connectionFactory;
        IConnection connection;
        static readonly ILog Logger = LogManager.GetLogger("RabbitMq");
        bool connectionFailed;
        Exception connectionFailedReason;
        bool disposed;
    }
}