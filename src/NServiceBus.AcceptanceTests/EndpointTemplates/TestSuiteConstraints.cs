﻿namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public interface ITestSuiteConstraints
    {
        bool SupportsDtc { get; }

        bool SupportsCrossQueueTransactions { get; }

        bool SupportsNativePubSub { get; }

        bool SupportsNativeDeferral { get; }

        bool SupportsOutbox { get; }

        IConfigureEndpointTestExecution TransportConfiguration { get; }

        IConfigureEndpointTestExecution PersistenceConfiguration { get; }
    }

    // ReSharper disable once PartialTypeWithSinglePart
    public partial class TestSuiteConstraints : ITestSuiteConstraints
    {
        public static TestSuiteConstraints Current = new TestSuiteConstraints();
    }
}