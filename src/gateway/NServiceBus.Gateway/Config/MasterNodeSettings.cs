namespace NServiceBus.Gateway.Config
{
    using Faults;
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport.Transactional;

    public class MasterNodeSettings:IMasterNodeSettings
    {
        readonly TransactionalTransport masterNodeTransport;
        readonly UnicastBus unicastBus;

        public MasterNodeSettings(TransactionalTransport masterNodeTransport,UnicastBus unicastBus)
        {
            this.masterNodeTransport = masterNodeTransport;
            this.unicastBus = unicastBus;
        }

        public IReceiveMessages Receiver
        {
            get
            {
                //todo - use the type + IBuilder to get a fresh instance. This requires the MsmqMessageReceiver to be configured as singlecall, check with Udi
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
                return unicastBus.ForwardReceivedMessagesTo;
            }
        }
    }
}