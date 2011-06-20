using System;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport;
using NServiceBus.Config;
using NServiceBus.MasterNode;
using System.Threading;

namespace NServiceBus.Distributor
{
    public class ReadyMessageManager : INeedInitialization, IWantToRunWhenConfigurationIsComplete
    {
        /// <summary>
        /// No DI available here.
        /// </summary>
        public void Init()
        {
            if (RoutingConfig.IsConfiguredAsMasterNode)
                ControlQueue = Address.Local.SubScope(Configurer.DistributorControlName);

        }

        public IManageTheMasterNode masterNodeManager { get; set; }
        public IStartableBus Bus { get; set; }
        public ITransport EndpointTransport { get; set; }
        public IBus EndpointBus { get; set; }

        public int NumberOfWorkerThreads { get; set; }
        private static Address ControlQueue { get; set; }

        public void Run()
        {

            Bus.Started += (x, y) => 
                ThreadPool.QueueUserWorkItem(s =>
                    {
                        while (ControlQueue == null)
                            ControlQueue = masterNodeManager.GetMasterNode(); // has built in sleep

                        ControlQueue = ControlQueue.SubScope(Configurer.DistributorControlName);

                        SendReadyMessage(true);

                        EndpointTransport.FinishedMessageProcessing += (a, b) => SendReadyMessage(false);
                    }
                );
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
