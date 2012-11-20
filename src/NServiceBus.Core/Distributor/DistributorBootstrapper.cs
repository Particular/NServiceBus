namespace NServiceBus.Distributor
{
    using Faults;
    using NServiceBus.Config;
    using ObjectBuilder;
    using Unicast.Distributor;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport.Transactional;
    using Unicast.Transport;

    public class DistributorBootstrapper : IWantToRunWhenBusStartsAndStops
    {
        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }
        public IBuilder Builder { get; set; }

        public Address InputQueue { get; set; }

        public void Stop()
        {
            if (distributor != null)
                distributor.Stop();
        }

        public void Start()
        {
            if (!Configure.Instance.DistributorConfiguredToRunOnThisEndpoint())
                return;
           
          
            distributor = new Unicast.Distributor.Distributor
            {
                MessageBusTransport = Builder.Build<ITransport>(),
                MessageSender = new MsmqMessageSender(),
                WorkerManager = WorkerAvailabilityManager,
                DataTransportInputQueue = InputQueue
            };
            
            LicenseConfig.CheckForLicenseLimitationOnNumberOfWorkerNodes();
            
            distributor.Start();
        }

        Unicast.Distributor.Distributor distributor;
    }
}
