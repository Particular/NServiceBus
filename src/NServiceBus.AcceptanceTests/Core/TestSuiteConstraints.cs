namespace NServiceBus.AcceptanceTests;

using AcceptanceTesting.Support;
using NUnit.Framework;

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

    public bool SupportsPurgeOnStartup => true;

    public IConfigureEndpointTestExecution CreateTransportConfiguration()
        => new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsDelayedDelivery, enforcePublisherMetadata: EnforcePublisherMetadata);

    // Making sure all tests shipped to down streams have the necessary publisher metadata available but exclude
    // the ones in the Core folder since they are not shipped to down streams.
    static bool EnforcePublisherMetadata =>
        TestContext.CurrentContext.Test.Namespace != null &&
        !TestContext.CurrentContext.Test.Namespace.StartsWith("NServiceBus.AcceptanceTests.Core.");

    public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointAcceptanceTestingPersistence();
}