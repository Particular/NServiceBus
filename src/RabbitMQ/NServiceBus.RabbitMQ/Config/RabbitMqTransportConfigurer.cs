namespace NServiceBus.Transport.RabbitMQ.Config
{
    using NServiceBus.Config;

    public class RabbitMqTransportConfigurer : IConfigureTransport<NServiceBus.RabbitMQ>
    {
        public void Configure(Configure config)
        {
            config.RabbitMQTransport();
        }
    }
}