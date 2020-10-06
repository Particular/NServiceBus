namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => false;
        public bool SupportsCrossQueueTransactions => true;
        public bool SupportsNativePubSub => true;
        public bool SupportsNativeDeferral => true; //TODO how to test inmem subscription/timeout storage?
        public bool SupportsOutbox => true;
        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointAcceptanceTestingTransport();
        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointInMemoryPersistence();
    }
}