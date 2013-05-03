namespace NServiceBus
{
    using Transports;

    public class ActiveMQ : TransportDefinition
    {
        public override bool HasNativePubSubSupport { get { return true; } }
    }
}