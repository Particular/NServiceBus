namespace EasyNetQ
{
    using System;
    using RabbitMQ.Client;

    public interface IPersistentConnection : IDisposable
    {
        event Action Connected;
        event Action Disconnected;
        bool IsConnected { get; }
        IModel CreateModel();
    }
}