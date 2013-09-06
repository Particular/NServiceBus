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

        public IConnection GetPublishConnection()
        {
            //note: The purpose is there so that we/users can add more advanced connection managers in the future
            lock (connectionFactory)
            {
                return connectionPublish ?? (connectionPublish = new PersistentConnection(connectionFactory, connectionConfiguration.RetryDelay));
            }
        }

        public IConnection GetConsumeConnection()
        {
            //note: The purpose is there so that we/users can add more advanced connection managers in the future
            lock (connectionFactory)
            {
                return connectionConsume ?? (connectionConsume = new PersistentConnection(connectionFactory, connectionConfiguration.RetryDelay));
            }
        }

        public IConnection GetAdministrationConnection()
        {
            //note: The purpose is there so that we/users can add more advanced connection managers in the future
            lock (connectionFactory)
            {
                return connectionAdministration ?? (connectionAdministration = new PersistentConnection(connectionFactory, connectionConfiguration.RetryDelay));
            }
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {

            if (connectionConsume != null)
            {
                connectionConsume.Dispose();
            }
            if (connectionAdministration != null)
            {
                connectionAdministration.Dispose();
            }
            if (connectionPublish != null)
            {
                connectionPublish.Dispose();
            }
        }

        IConnectionFactory connectionFactory;
        IConnectionConfiguration connectionConfiguration;
        PersistentConnection connectionConsume;
        PersistentConnection connectionAdministration;
        PersistentConnection connectionPublish;
    }
}