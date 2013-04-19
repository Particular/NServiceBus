namespace NServiceBus.Transports.RabbitMQ
{
    using global::RabbitMQ.Client;

    public interface IManageRabbitMqConnections
    {
        IConnection GetConnection(ConnectionPurpose purpose);
    }

    public enum ConnectionPurpose
    {
        Publish=1,
        Consume=2,
        Administration = 3
    }
}