namespace NServiceBus.HybridSubscriptions.Tests
{
    class EndpointWithNativePubSub : DefaultServer
    {
        public EndpointWithNativePubSub() : base(true)
        {
        }
    }
}