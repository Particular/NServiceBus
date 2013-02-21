namespace NServiceBus.RabbitMq
{
    using global::RabbitMQ.Client;

    public interface IManageRabbitMqConnections
    {
        IConnection GetConnection(ConnectionPurpose purpose,string callerId);
    }

    public enum ConnectionPurpose
    {
        Publish=1,
        Consume=2,
        Administration = 3
    }
}