namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => false;
        public bool SupportsCrossQueueTransactions => true;
        public bool SupportsNativePubSub => false;
        public bool SupportsNativeDeferral => true;
        public bool SupportsOutbox => false;
        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsNativeDeferral);
        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointLearningPersistence();
    }
}