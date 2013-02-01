namespace NServiceBus.RabbitMq
{
    using System;
    using System.Threading;
    using Logging;
    using global::RabbitMQ.Client;

    public class RabbitMqConnectionManager : IDisposable
    {
        public int MaxRetries { get; set; }

        public TimeSpan DelayBetweenRetries { get; set; }

        public RabbitMqConnectionManager(ConnectionFactory connectionFactory)
        {
            MaxRetries = 2;
            DelayBetweenRetries = TimeSpan.FromSeconds(2);

            this.connectionFactory = connectionFactory;
        }

        public IConnection GetConnection()
        {
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
                    return connectionFactory.CreateConnection();
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Tells the transport to dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (connection == null)
                return;

            if (connection.IsOpen)
                connection.Close();

            connection.Dispose();
            connection = null;
        }

        readonly ConnectionFactory connectionFactory;
        IConnection connection;
        static readonly ILog Logger = LogManager.GetLogger("RabbitMq");
        bool connectionFailed;
        Exception connectionFailedReason;
    }
}