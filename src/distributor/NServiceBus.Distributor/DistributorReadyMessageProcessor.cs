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
    public class DistributorReadyMessageProcessor:IWantToRunWhenConfigurationIsComplete
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
                    var messages = Configure.Instance.Builder.Build<IMessageSerializer>()
                        .Deserialize(new MemoryStream(ev.Message.Body));
                    foreach (var msg in messages)
                        if (msg is ReadyMessage)
                            Handle(msg as ReadyMessage, ev.Message.ReplyToAddress);
                };

            var bus = Configure.Instance.Builder.Build<IStartableBus>();
            bus.Started += (obj, ev) => controlTransport.Start(ControlQueue);
        }

        private void Handle(ReadyMessage message, Address returnAddress)
        {
            Configurer.Logger.Info("Server available: " + returnAddress);

            if (message.ClearPreviousFromThisAddress) //indicates worker started up
                WorkerAvailabilityManager.ClearAvailabilityForWorker(returnAddress);

            WorkerAvailabilityManager.WorkerAvailable(returnAddress);
        }

        ITransport controlTransport;
        
    }
}
