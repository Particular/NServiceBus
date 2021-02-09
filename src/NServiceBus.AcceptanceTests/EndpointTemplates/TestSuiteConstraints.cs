namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public interface ITestSuiteConstraints
    {
        bool SupportsDtc { get; }

        bool SupportsCrossQueueTransactions { get; }

        bool SupportsNativePubSub { get; }

        bool SupportsDelayedDelivery { get; }

        bool SupportsOutbox { get; }

        bool SupportsPurgeOnStartup { get; }

        IConfigureEndpointTestExecution CreateTransportConfiguration();

        IConfigureEndpointTestExecution CreatePersistenceConfiguration();
    }

    public partial class TestSuiteConstraints : ITestSuiteConstraints
    {
        public static TestSuiteConstraints Current = new TestSuiteConstraints();
    }
}