using RabbitMQ.Client;

namespace EasyNetQ
{
    using NServiceBus.Transports.RabbitMQ.Config;

    public interface IConnectionFactory
    {
        IConnection CreateConnection();
        IConnectionConfiguration Configuration { get; }
        IHostConfiguration CurrentHost { get; }
        bool Next();
        void Success();
        void Reset();
        bool Succeeded { get; }
    }
}