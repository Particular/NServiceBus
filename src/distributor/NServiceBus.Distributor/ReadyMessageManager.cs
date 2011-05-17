using System;
using System.IO;
using NServiceBus.Faults;
using NServiceBus.Grid.Messages;
using NServiceBus.Serialization;
using NServiceBus.Unicast.Distributor;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Distributor
{
    public class ReadyMessageManager : IDisposable
    {
        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }
        public int NumberOfWorkerThreads { get; set; }
        public Address ControlQueue { get; set; }

        public void Init()
        {
            endpointTransport = Configure.Instance.Builder.Build<ITransport>();
            endpointTransport.FinishedMessageProcessing += (x, y) => SendReadyMessage(false);

            var bus = Configure.Instance.Builder.Build<IStartableBus>();
            bus.Started += (x, y) =>
                                {
                                    SendReadyMessage(true);
                                    controlTransport.Start(ControlQueue);
                                };

            endpointBus = Configure.Instance.Builder.Build<IBus>();

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

        public void SendReadyMessage(bool startup)
        {
            IMessage[] messages;
            if (startup)
            {
                messages = new IMessage[endpointTransport.NumberOfWorkerThreads];
                for (var i = 0; i < endpointTransport.NumberOfWorkerThreads; i++)
                {
                    var rm = new ReadyMessage
                    {
                        ClearPreviousFromThisAddress = (i == 0)
                    };

                    messages[i] = rm;
                }
            }
            else
            {
                messages = new IMessage[] {new ReadyMessage()};
            }

            endpointBus.Send(ControlQueue, messages);
        }

        public void Dispose()
        {
            if (controlTransport != null)
                controlTransport.Dispose();
        }

        private ITransport controlTransport;
        private ITransport endpointTransport;
        private IBus endpointBus;
    }
}
