namespace NServiceBus.Gateway.Config
{
    using Faults;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport.Transactional;

    public class MasterNodeSettings:IMasterNodeSettings
    {
        readonly TransactionalTransport masterNodeTransport;

        public MasterNodeSettings(TransactionalTransport masterNodeTransport)
        {
            this.masterNodeTransport = masterNodeTransport;
        }

        public IReceiveMessages Receiver
        {
            get
            {
                //todo - make this configurable
                return new MsmqMessageReceiver();
            }
        }

        public int NumberOfWorkerThreads
        {
            get
            {
                if (masterNodeTransport.NumberOfWorkerThreads == 0)
                    return 1;

                return masterNodeTransport.NumberOfWorkerThreads;
            }
        }

        public int MaxRetries
        {
            get
            {
                return masterNodeTransport.MaxRetries;
            }
        }

        public IManageMessageFailures FailureManager
        {
            get
            {
                return masterNodeTransport.FailureManager;
            }
        }

        public string AddressOfAuditStore
        {
            get
            {
                return null; //todo - get this from the unicast bus
            }
        }
    }
}