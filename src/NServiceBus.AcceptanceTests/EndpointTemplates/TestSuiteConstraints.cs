namespace NServiceBus.AcceptanceTests;

using AcceptanceTesting.Support;

public interface ITestSuiteConstraints
{
    bool SupportsDtc { get; }

    bool SupportsCrossQueueTransactions { get; }

    bool SupportsNativePubSub { get; }

    bool SupportsDelayedDelivery { get; }

    bool SupportsOutbox { get; }

    bool SupportsPurgeOnStartup { get; }

    // Defaulted to true, so that existing downstream implementations of
    // TestSuiteConstraints that predate this flag keep compiling when they pick up a newer
    // NServiceBus.AcceptanceTests.Sources package
    bool SupportsTTBR => true;

    IConfigureEndpointTestExecution CreateTransportConfiguration();

    IConfigureEndpointTestExecution CreatePersistenceConfiguration();
}

public partial class TestSuiteConstraints : ITestSuiteConstraints
{
    // Typed as the interface, not the concrete class, so that a flag with a default
    // implementation (such as SupportsTTBR above) resolves through interface dispatch for every
    // caller. A property that exists only on TestSuiteConstraints itself, and not on the
    // interface, would not be reachable through Current if it were typed as TestSuiteConstraints.
    public static ITestSuiteConstraints Current { get; } = new TestSuiteConstraints();
}