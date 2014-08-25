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

        public override string GetSubScope(string address, string qualifier)
        {
            return address + "." + qualifier;
        }
    }
}
