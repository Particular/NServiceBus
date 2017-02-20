namespace NServiceBus.AcceptanceTests
{
    public partial class TestSuiteConstraints
    {
        public bool SupportDtc => true;
        public bool SupportCrossQueueTransactions => true;
        public bool SupportNativePubSub => false;
        public bool SupportNativeDeferral => false;
        public bool ProvideOutboxPersistence => true;
    }
}