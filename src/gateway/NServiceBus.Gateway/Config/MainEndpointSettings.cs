namespace NServiceBus.Gateway.Config
{
    using Faults;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport.Transactional;

    public class MainEndpointSettings:IMainEndpointSettings
    {
        readonly TransactionalTransport masterNodeTransport;
        readonly UnicastBus unicastBus;

        public IBuilder Builder { get; set; }


        public MainEndpointSettings(TransactionalTransport masterNodeTransport,UnicastBus unicastBus)
        {
            this.masterNodeTransport = masterNodeTransport;
            this.unicastBus = unicastBus;
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


        public IManageMessageFailures FailureManager
        {
            get
            {
                return Builder.Build(masterNodeTransport.FailureManager.GetType()) as IManageMessageFailures;
            }
        }

        public Address AddressOfAuditStore
        {
            get
            {
                return unicastBus.ForwardReceivedMessagesTo;
            }
        }
    }
}