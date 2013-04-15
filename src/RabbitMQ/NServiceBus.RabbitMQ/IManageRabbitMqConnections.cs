namespace NServiceBus.Transports.RabbitMQ
{
    using EasyNetQ;
    using global::RabbitMQ.Client;

    public interface IManageRabbitMqConnections
    {
        IPersistentConnection GetConnection(ConnectionPurpose purpose);
    }

    public enum ConnectionPurpose
    {
        Publish=1,
        Consume=2,
        Administration = 3
    }
}