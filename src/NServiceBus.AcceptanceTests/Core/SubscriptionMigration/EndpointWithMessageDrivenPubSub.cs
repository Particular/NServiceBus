namespace NServiceBus.HybridSubscriptions.Tests
{
    class EndpointWithMessageDrivenPubSub : AcceptanceTestingTransportServer
    {
        public EndpointWithMessageDrivenPubSub() : base(false)
        {
        }
    }
}