namespace NServiceBus.Distributor
{
    using Config;
    using Unicast.Distributor;
    using Unicast.Transport.Transactional;
    using Unicast.Queuing.Msmq;
    using Faults;
    using Unicast.Transport;

    public class DistributorReadyMessageProcessor : IWantToRunWhenConfigurationIsComplete
    {
        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }
        public IManageMessageFailures MessageFailureManager { get; set; }
        public int NumberOfWorkerThreads { get; set; }

        public Address ControlQueue { get; set; }

        public void Run()
        {
            if (!DistributorSetup.DistributorShouldRunOnThisEndpoint())
                return;
        
            controlTransport = new TransactionalTransport
            {
                IsTransactional = true,
                FailureManager = MessageFailureManager,
                MessageReceiver = new MsmqMessageReceiver(),
                MaxRetries = 1,
                NumberOfWorkerThreads = NumberOfWorkerThreads,
            };

            controlTransport.TransportMessageReceived +=
                (obj, ev) =>
                {
                    var transportMessage = ev.Message;

                    if (!transportMessage.Headers.ContainsKey(Headers.ControlMessage))
                        return;

                    HandleControlMessage(transportMessage);
                };

            var bus = Configure.Instance.Builder.Build<IStartableBus>();
            bus.Started += (obj, ev) => controlTransport.Start(ControlQueue);
        }

        void HandleControlMessage(TransportMessage controlMessage)
        {
            var returnAddress = controlMessage.ReplyToAddress;
            DistributorSetup.Logger.Debug("Worker available: " + returnAddress);

            if (controlMessage.Headers.ContainsKey(Headers.WorkerStarting))
                WorkerAvailabilityManager.ClearAvailabilityForWorker(returnAddress);

            if(controlMessage.Headers.ContainsKey(Headers.WorkerCapacityAvailable))
            {
                var capacity = int.Parse(controlMessage.Headers[Headers.WorkerCapacityAvailable]);
                    
                WorkerAvailabilityManager.WorkerAvailable(returnAddress,capacity);
            }
            
        }

        ITransport controlTransport;

    }
}
