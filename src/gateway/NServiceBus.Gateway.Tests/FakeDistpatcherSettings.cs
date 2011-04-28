namespace NServiceBus.Gateway.Tests
{
    using Config;
    using Faults;
    using Unicast.Queuing;

    public class FakeDistpatcherSettings : IMasterNodeSettings
    {
        public IReceiveMessages Receiver{ get; set;}
        

        public int NumberOfWorkerThreads
        {
            get { return 1; }
        }

        public int MaxRetries
        {
            get { return 1; }
        }

        public IManageMessageFailures FailureManager
        {
            get { return null; }
        }

        public string AddressOfAuditStore
        {
            get { return null; }
        }
    }
}