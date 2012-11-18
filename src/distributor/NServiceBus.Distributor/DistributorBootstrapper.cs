using NServiceBus.Unicast.Distributor;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport.Transactional;
using NServiceBus.Config;

namespace NServiceBus.Distributor
{
    using Faults;
    using ObjectBuilder;
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
