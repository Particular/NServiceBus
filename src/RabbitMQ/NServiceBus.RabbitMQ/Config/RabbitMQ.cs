namespace NServiceBus
{
    using Transports;

    public class RabbitMQ : TransportDefinition
    {
        public RabbitMQ()
        {
            HasNativePubSubSupport = true;
            HasSupportForCentralizedPubSub = true;
        }
    }
}