namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => false;

        public bool SupportsCrossQueueTransactions => true;

        // Disable native pub-sub for tests that require message-driven pub-sub. The tests in the "Core"
        // folder are not shipped to downstreams and therefore are only executed on this test project and
        // "NServiceBus.Learning.AcceptanceTests" (which is running the tests using native pub-sub).
        public bool SupportsNativePubSub => false;

        public bool SupportsDelayedDelivery => false;

        public bool SupportsOutbox => true;

        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsDelayedDelivery);

        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointAcceptanceTestingPersistence();
    }
}