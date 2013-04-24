namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using global::RabbitMQ.Client;

    public interface IPersistentConnection
    {
        event Action Connected;
        event Action Disconnected;
        bool IsConnected { get; }
        IModel CreateModel();
    }
}