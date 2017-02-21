namespace NServiceBus.AcceptanceTests
{
    public interface ITestSuiteConstraints
    {
        bool SupportsDtc { get; }

        bool SupportsCrossQueueTransactions { get; }

        bool SupportsNativePubSub { get; }

        bool SupportsNativeDeferral { get; }

        bool SupportsOutbox { get; }
    }

    // ReSharper disable once PartialTypeWithSinglePart
    public partial class TestSuiteConstraints : ITestSuiteConstraints
    {

    }
}