namespace NServiceBus.Core.Tests.Fakes
{
    using Transports;

    public class FakeCentralizedPubSubTransportDefinition : TransportDefinition
    {
        public FakeCentralizedPubSubTransportDefinition()
        {
            HasNativePubSubSupport = true;
            HasSupportForCentralizedPubSub = true;
        }
    }
}
