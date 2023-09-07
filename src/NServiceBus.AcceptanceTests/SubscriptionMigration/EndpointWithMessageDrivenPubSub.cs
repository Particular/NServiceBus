namespace NServiceBus.AcceptanceTests.Core.SubscriptionMigration
{
    class EndpointWithMessageDrivenPubSub : AcceptanceTestingTransportServer
    {
        public EndpointWithMessageDrivenPubSub() : base(false)
        {
        }
    }
}