using System;
using System.IO;
using NServiceBus.Faults;
using NServiceBus.Grid.Messages;
using NServiceBus.Serialization;
using NServiceBus.Unicast.Distributor;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Transport.Transactional;
using NServiceBus.Config;
using NServiceBus.MasterNode;

namespace NServiceBus.Distributor
{
    public class ReadyMessageManager : IWantToRunWhenConfigurationIsComplete
    {
        public IManageTheMasterNode masterNodeManager { get; set; }
        public IStartableBus Bus { get; set; }
        public ITransport EndpointTransport { get; set; }
        public IBus EndpointBus { get; set; }

        public int NumberOfWorkerThreads { get; set; }
        public Address ControlQueue { get; set; }

        public void Run()
        {
            Bus.Started += (x, y) => SendReadyMessage(true);

            EndpointTransport.FinishedMessageProcessing += (x, y) => SendReadyMessage(false);
        }

        public void SendReadyMessage(bool startup)
        {
            IMessage[] messages;
            if (startup)
            {
                messages = new IMessage[EndpointTransport.NumberOfWorkerThreads];
                for (var i = 0; i < EndpointTransport.NumberOfWorkerThreads; i++)
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

            EndpointBus.Send(ControlQueue, messages);
        }
    }
}
