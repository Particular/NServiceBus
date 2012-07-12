using NServiceBus.Logging;

namespace NServiceBus.Distributor
{
    using ReadyMessages;
    using Unicast.Distributor;
    using Unicast.Transport.Transactional;
    using Unicast.Queuing.Msmq;
    using Faults;
    using Unicast.Transport;

    public class DistributorReadyMessageProcessor : IWantToRunWhenBusStartsAndStops
    {
        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }
        public IManageMessageFailures MessageFailureManager { get; set; }
        public int NumberOfWorkerThreads { get; set; }

        public Address ControlQueue { get; set; }

        public void Start()
        {
            if (!Configure.Instance.DistributorConfiguredToRunOnThisEndpoint())
                return;
        
            controlTransport = new TransactionalTransport
            {
                IsTransactional = !ConfigureVolatileQueues.IsVolatileQueues,
                FailureManager = MessageFailureManager,
                MessageReceiver = new MsmqMessageReceiver(),
                MaxRetries = 5,
                NumberOfWorkerThreads = NumberOfWorkerThreads,
            };

            controlTransport.TransportMessageReceived +=
                (obj, ev) =>
                {
                    var transportMessage = ev.Message;

                    if (!transportMessage.IsControlMessage())
                        return;

                    HandleControlMessage(transportMessage);
                };

            controlTransport.Start(ControlQueue);
        }

        public void Stop()
        {
            //TODO: Need to add code here
        }

        void HandleControlMessage(TransportMessage controlMessage)
        {
            var replyToAddress = controlMessage.ReplyToAddress;

            if (LicenseConfig.LimitNumberOfWorkers(replyToAddress))
                return;
            
            if (controlMessage.Headers.ContainsKey(Headers.WorkerStarting))
            {
                WorkerAvailabilityManager.ClearAvailabilityForWorker(replyToAddress);
                Logger.InfoFormat("Worker {0} has started up, clearing previous reported capacity", replyToAddress);
            }

            if(controlMessage.Headers.ContainsKey(Headers.WorkerCapacityAvailable))
            {
                var capacity = int.Parse(controlMessage.Headers[Headers.WorkerCapacityAvailable]);
                    
                WorkerAvailabilityManager.WorkerAvailable(replyToAddress,capacity);

                Logger.InfoFormat("Worker {0} checked in with available capacity: {1}", replyToAddress, capacity);
            }
        }

        ITransport controlTransport;
        static readonly ILog Logger = LogManager.GetLogger("Distributor."+Configure.EndpointName);
    }
}
