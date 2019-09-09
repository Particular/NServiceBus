namespace NServiceBus.HybridSubscriptions.Tests
{
    class EndpointWithNativePubSub : AcceptanceTestingTransportServer
    {
        public EndpointWithNativePubSub() : base(true)
        {
        }
    }
}