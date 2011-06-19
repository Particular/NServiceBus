using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    class DistributorReadyMessageProcessor
    {
        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }
        public int NumberOfWorkerThreads { get; set; }

        public void Init()
        {
            controlTransport = new TransactionalTransport
            {
                IsTransactional = true,
                FailureManager = Configure.Instance.Builder.Build<IManageMessageFailures>(),
                MessageReceiver = new MsmqMessageReceiver(),
                MaxRetries = 1,
                NumberOfWorkerThreads = NumberOfWorkerThreads
            };

            var serializer = Configure.Instance.Builder.Build<IMessageSerializer>();

            controlTransport.TransportMessageReceived +=
                (obj, ev) =>
                {
                    var messages = serializer.Deserialize(new MemoryStream(ev.Message.Body));
                    foreach (var msg in messages)
                        if (msg is ReadyMessage)
                            Handle(msg as ReadyMessage, ev.Message.ReplyToAddress);
                };
        }

        private void Handle(ReadyMessage message, Address returnAddress)
        {
            Configurer.Logger.Info("Server available: " + returnAddress);

            if (message.ClearPreviousFromThisAddress) //indicates worker started up
                WorkerAvailabilityManager.ClearAvailabilityForWorker(returnAddress);

            WorkerAvailabilityManager.WorkerAvailable(returnAddress);
        }

        private ITransport controlTransport;
    }
}
