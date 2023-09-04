namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public class TestSuiteConstraints : ITestSuiteConstraints
    {
        public bool SupportsDtc => false;

        public bool SupportsCrossQueueTransactions => true;

        public bool SupportsNativePubSub => true;

        public bool SupportsDelayedDelivery => true;

        public bool SupportsOutbox => false;

        public bool SupportsPurgeOnStartup => true;

        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointLearningTransport();

        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointLearningPersistence();
    }
}