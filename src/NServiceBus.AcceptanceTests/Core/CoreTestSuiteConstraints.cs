namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    public partial class TestSuiteConstraints
    {
        public bool SupportDtc => true;
        public bool SupportCrossQueueTransactions => true;
        public bool SupportNativePubSub => false;
        public bool SupportNativeDeferral => false;
    }
}