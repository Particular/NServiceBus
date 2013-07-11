namespace NServiceBus
{
    using Transports;

    public class ActiveMQ : TransportDefinition
    {
        public ActiveMQ()
        {
            HasNativePubSubSupport = true;
            HasSupportForCentralizedPubSub = true;
        }
    }
}