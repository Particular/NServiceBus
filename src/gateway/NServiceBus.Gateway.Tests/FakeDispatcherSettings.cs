namespace NServiceBus.Gateway.Tests
{
    using Config;
    using Faults;
    using Unicast.Queuing;

    public class FakeDispatcherSettings : IMainEndpointSettings
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

        public IManageMessageFailures FailureManager { get; set; }

        public Address AddressOfAuditStore
        {
            get { return Address.Undefined; }
        }
    }
}