using System;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ
{
    using System.Collections;
    using RabbitMQ.Client.Events;

    /// <summary>
    /// A connection that attempts to reconnect if the inner connection is closed.
    /// </summary>
    public class PersistentConnection : IPersistentConnection, IConnection
    {
        private const int connectAttemptIntervalMilliseconds = 5000;

        private readonly IConnectionFactory connectionFactory;
        private readonly IEasyNetQLogger logger;

        public PersistentConnection(IConnectionFactory connectionFactory, IEasyNetQLogger logger)
        {
            this.connectionFactory = connectionFactory;
            this.logger = logger;

            TryToConnect(null);
        }

        public event Action Connected;
        public event Action Disconnected;

        public IModel CreateModel()
        {
            if(!IsConnected)
            {
                throw new EasyNetQException("Rabbit server is not connected.");
            }
            return connection.CreateModel();
        }

        public bool IsConnected
        {
            get { return connection != null && connection.IsOpen && !disposed; }
        }

        public void Close() {
            connection.ConnectionShutdown -= OnConnectionShutdown;
            connection.Close();    
        }

        public void Close(int timeout) {
            connection.ConnectionShutdown -= OnConnectionShutdown;
            connection.Close(timeout);
        }

        IConnection connection;

        void StartTryToConnect()
        {
            var timer = new Timer(TryToConnect);
            timer.Change(connectAttemptIntervalMilliseconds, Timeout.Infinite);
        }

        void TryToConnect(object timer)
        {
            if(timer != null) ((Timer) timer).Dispose();

            logger.DebugWrite("Trying to connect");
            if (disposed) return;

            connectionFactory.Reset();
            do
            {
                try
                {
                    connection = connectionFactory.CreateConnection();
                    connectionFactory.Success();
                }
                catch (System.Net.Sockets.SocketException socketException)
                {
                    LogException(socketException);
                }
                catch (BrokerUnreachableException brokerUnreachableException)
                {
                    LogException(brokerUnreachableException);
                }
            } while (connectionFactory.Next());

            if (connectionFactory.Succeeded)
            {
                connection.ConnectionShutdown += OnConnectionShutdown;

                OnConnected();
                logger.InfoWrite("Connected to RabbitMQ. Broker: '{0}', Port: {1}, VHost: '{2}'",
                    connectionFactory.CurrentHost.Host,
                    connectionFactory.CurrentHost.Port,
                    connectionFactory.Configuration.VirtualHost);
            }
            else
            {
                logger.ErrorWrite("Failed to connected to any Broker. Retrying in {0} ms\n", 
                    connectAttemptIntervalMilliseconds);
                StartTryToConnect();
            }
        }

        void LogException(Exception exception)
        {
            logger.ErrorWrite("Failed to connect to Broker: '{0}', Port: {1} VHost: '{2}'. " +
                    "ExceptionMessage: '{3}'",
                connectionFactory.CurrentHost.Host,
                connectionFactory.CurrentHost.Port,
                connectionFactory.Configuration.VirtualHost,
                exception.Message);
        }

        void OnConnectionShutdown(IConnection _, ShutdownEventArgs reason)
        {
            if (disposed) return;
            OnDisconnected();

            // try to reconnect and re-subscribe
//            logger.InfoWrite("Disconnected from RabbitMQ Broker");
            // HACK: pulling EasyNetQ into NSB
            logger.InfoWrite("Disconnected from RabbitMQ Broker, reason: {0} , going to reconnect",reason);

            TryToConnect(null);
        }

        public void OnConnected()
        {
            logger.DebugWrite("OnConnected event fired");
            if (Connected != null) Connected();
        }

        public void OnDisconnected()
        {
            if (Disconnected != null) Disconnected();
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (connection != null) connection.Dispose();
        }

        public void Abort() {
            connection.Abort();
        }

        public void Abort(ushort reasonCode, string reasonText) {
            connection.Abort(reasonCode, reasonText);
        }

        public void Abort(int timeout) {
            connection.Abort(timeout);
        }

        public void Abort(ushort reasonCode, string reasonText, int timeout) {
            connection.Abort(reasonCode, reasonText, timeout);
        }

        public AmqpTcpEndpoint Endpoint {
            get { return connection.Endpoint; }
        }

        public IProtocol Protocol {
            get { return connection.Protocol; }
        }

        public ushort ChannelMax {
            get { return connection.ChannelMax; }
        }

        public uint FrameMax {
            get { return connection.FrameMax; }
        }

        public ushort Heartbeat {
            get { return connection.Heartbeat; }
        }

        public IDictionary ClientProperties {
            get { return connection.ClientProperties; }
        }

        public IDictionary ServerProperties {
            get { return connection.ServerProperties; }
        }

        public AmqpTcpEndpoint[] KnownHosts {
            get { return connection.KnownHosts; }
        }

        public ShutdownEventArgs CloseReason {
            get { return connection.CloseReason; }
        }

        public bool IsOpen {
            get { return connection.IsOpen; }
        }

        public bool AutoClose {
            get { return connection.AutoClose; }
            set { connection.AutoClose = value; }
        }

        public IList ShutdownReport {
            get { return connection.ShutdownReport; }
        }

        public event ConnectionShutdownEventHandler ConnectionShutdown {
            add { connection.ConnectionShutdown += value; }
            remove { connection.ConnectionShutdown -= value; }
        }

        public event CallbackExceptionEventHandler CallbackException {
            add { connection.CallbackException += value; }
            remove { connection.CallbackException -= value; }
        }

        public void Close(ushort reasonCode, string reasonText, int timeout) {
            connection.Close(reasonCode, reasonText, timeout);
        }

        public void Close(ushort reasonCode, string reasonText) {
            connection.Close(reasonCode, reasonText);
        }
    }
}