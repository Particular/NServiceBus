namespace NServiceBus.AcceptanceTests
{
    public interface ITestSuiteConstraints
    {
        bool SupportDtc { get; }

        bool SupportCrossQueueTransactions { get; }

        bool SupportNativePubSub { get; }

        bool SupportNativeDeferral { get; }
    }

    // ReSharper disable once PartialTypeWithSinglePart
    public partial class TestSuiteConstraints : ITestSuiteConstraints
    {

    }
}