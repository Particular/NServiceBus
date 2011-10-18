using NServiceBus.Unicast.Distributor;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport.Transactional;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Faults;
using NServiceBus.Unicast.Transport;
using NServiceBus.Serialization;
using System.IO;

namespace NServiceBus.Distributor
{
    using Config;

    //todo 
    public class DistributorReadyMessageProcessor : IWantToRunWhenConfigurationIsComplete
    {
        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }
        public IManageMessageFailures MessageFailureManager { get; set; }
        public int NumberOfWorkerThreads { get; set; }

        public Address ControlQueue { get; set; }

        public void Run()
        {
            if (!RoutingConfig.IsConfiguredAsMasterNode)
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
            Configurer.Logger.Debug("Worker available: " + returnAddress);

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
