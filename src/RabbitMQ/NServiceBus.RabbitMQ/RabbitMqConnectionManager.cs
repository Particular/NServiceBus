namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
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

                return connection ?? (connection = TryCreateConnection());
//                if (!connections.ContainsKey(purpose)) {
//                    Logger.Info(string.Format("Opening up {0} connection",purpose));
//                    connections.Add(purpose, TryCreateConnection());
//                }
//
//                var connection = connections[purpose];
//                return connection;
            }
        }

        PersistentConnection TryCreateConnection()
        {
            int retries = 0;
            Exception exception = null;

            do
            {
                try
                {
                    if (retries > 0)
                    {
                        Thread.Sleep(connectionConfiguration.DelayBetweenRetries);
                        Logger.InfoFormat("Issuing retry attempt {0}", retries);
                    }

                    var connection = new PersistentConnection(connectionFactory, new EasyNetQLogger(Logger));

                    if(retries > 0)
                        Logger.InfoFormat("Connection to {0} re-established",connectionFactory.CurrentHost.Host);

                    return connection;
                }
                catch (Exception ex)
                {
                    connectionFailedReason = ex;
                    retries++;

                    Logger.Warn("Failed to connect to RabbitMq broker - " + connectionFactory.CurrentHost.Host, ex);
                }

                
            }
            while (retries <= connectionConfiguration.MaxRetries);
            
            connectionFailed = true;

            Configure.Instance.RaiseCriticalError("Failed to connect to the RabbitMq broker.", exception);

            throw exception;

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

                if (connection.IsConnected)
                {
                    connection.Close();
                }

                connection.Dispose();
                connection = null;

//                if (connections == null)
//                {
//                    return;
//                }
//
//                foreach (var persistentConnection in connections) {
//                    var connection = persistentConnection.Value;
//                if (connection == null)
//                {
//                    return;
//                }
//
//                if (connection.IsConnected)
//                {
//                    connection.Close();
//                }
//
//                connection.Dispose();
//                connection = null;
//                }

            }

            disposed = true;
        }

        ~RabbitMqConnectionManager()
        {
            Dispose(false);
        }

        readonly IConnectionFactory connectionFactory;
//        readonly ConnectionFactory connectionFactory;
        readonly IConnectionConfiguration connectionConfiguration;
        PersistentConnection connection;
//        readonly IDictionary<ConnectionPurpose, PersistentConnection> connections = new Dictionary<ConnectionPurpose, PersistentConnection>();
        static readonly ILog Logger = LogManager.GetLogger(typeof(RabbitMqConnectionManager));
        bool connectionFailed;
        Exception connectionFailedReason;
        bool disposed;
    }
}