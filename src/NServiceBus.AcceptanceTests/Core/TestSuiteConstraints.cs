namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => false;

        public bool SupportsCrossQueueTransactions => true;

        public bool SupportsNativePubSub => false;

        public bool SupportsDelayedDelivery => false;

        public bool SupportsOutbox => true;

        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsDelayedDelivery);

        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointAcceptanceTestingPersistence();
    }
}