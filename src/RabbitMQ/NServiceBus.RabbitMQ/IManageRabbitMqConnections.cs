namespace NServiceBus.Transports.RabbitMQ
{
    using global::RabbitMQ.Client;

    public interface IManageRabbitMqConnections
    {
        IConnection GetPublishConnection();
        IConnection GetConsumeConnection();
        IConnection GetAdministrationConnection();
    }

}