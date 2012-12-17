namespace NServiceBus.RabbitMQ.Config
{
    using NServiceBus.Config;

    public class RabbitMqTransportConfigurer : IConfigureTransport<Transports.RabbitMQ>
    {
        public void Configure(Configure config)
        {
            config.RabbitMqTransport();
        }
    }
}