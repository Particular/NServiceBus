using System;
using NServiceBus.Unicast.Distributor;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Distributor
{
    using Faults;
    using ObjectBuilder;

    public class DistributorBootstrapper : IWantToRunWhenBusStartsAndStops
    {
        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }
        public int NumberOfWorkerThreads { get; set; }
        public IManageMessageFailures MessageFailureManager { get; set; }
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
           
            var dataTransport = new TransactionalTransport
            {
                NumberOfWorkerThreads = NumberOfWorkerThreads,
                IsTransactional = !ConfigureVolatileQueues.IsVolatileQueues,
                MessageReceiver = new MsmqMessageReceiver(),
                MaxRetries = 5,
                FailureManager = Builder.Build(MessageFailureManager.GetType()) as IManageMessageFailures
            };

            distributor = new Unicast.Distributor.Distributor
            {
                MessageBusTransport = dataTransport,
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
