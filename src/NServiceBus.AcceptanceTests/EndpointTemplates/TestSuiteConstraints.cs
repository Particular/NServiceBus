namespace NServiceBus.AcceptanceTests;

using AcceptanceTesting.Support;

public interface ITestSuiteConstraints
{
    bool SupportsDtc { get; }

    bool SupportsCrossQueueTransactions { get; }

    bool SupportsNativePubSub { get; }

    // Automatic delayed retries that core's recoverability policy requests after a handler
    // fails. See https://github.com/Particular/NServiceBus/blob/master/docs/recoverability.md#dispatching-from-recoverability
    bool SupportsDelayedDelivery { get; }

    bool SupportsOutbox { get; }

    bool SupportsPurgeOnStartup { get; }

    // Delayed sends the application requests: a deferred send with DelayDeliveryWith or
    // DoNotDeliverBefore, and a saga timeout, which core sends to the saga's input queue.
    bool SupportsDelayedSends => true;

    IConfigureEndpointTestExecution CreateTransportConfiguration();

    IConfigureEndpointTestExecution CreatePersistenceConfiguration();
}

public partial class TestSuiteConstraints : ITestSuiteConstraints
{
    // Typed as the interface, not the concrete class, so that a flag with a default
    // implementation (such as SupportsDelayedSends above) resolves through interface dispatch for
    // every caller. A property that exists only on TestSuiteConstraints itself, and not on the
    // interface, would not be reachable through Current if it were typed as TestSuiteConstraints.
    public static ITestSuiteConstraints Current { get; } = new TestSuiteConstraints();
}