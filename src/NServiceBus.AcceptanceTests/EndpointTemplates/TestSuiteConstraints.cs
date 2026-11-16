namespace NServiceBus.AcceptanceTests;

using AcceptanceTesting.Support;

public interface ITestSuiteConstraints
{
    bool SupportsDtc { get; }

    bool SupportsCrossQueueTransactions { get; }

    bool SupportsNativePubSub { get; }

    bool SupportsDelayedDelivery { get; }

    // Defaults to SupportsDelayedDelivery so existing implementations run the same tests as before.
    bool SupportsDelayedRetries => SupportsDelayedDelivery;

    bool SupportsOutbox { get; }

    bool SupportsPurgeOnStartup { get; }

    IConfigureEndpointTestExecution CreateTransportConfiguration();

    IConfigureEndpointTestExecution CreatePersistenceConfiguration();
}

public partial class TestSuiteConstraints : ITestSuiteConstraints
{
    // Typed as the interface so that callers reach default interface members.
    public static ITestSuiteConstraints Current { get; } = new TestSuiteConstraints();
}