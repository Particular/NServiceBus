namespace NServiceBus.AcceptanceTests.Core.SubscriptionMigration
{
    class EndpointWithNativePubSub : AcceptanceTestingTransportServer
    {
        public EndpointWithNativePubSub() : base(true)
        {
        }
    }
}