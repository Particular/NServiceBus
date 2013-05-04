namespace NServiceBus
{
    using Transports;

    public class RabbitMQ : TransportDefinition
    {
        public override bool HasNativePubSubSupport { get { return true; } }
        public override bool HasSupportForCentralizedPubSub { get { return true; } }
    }
}