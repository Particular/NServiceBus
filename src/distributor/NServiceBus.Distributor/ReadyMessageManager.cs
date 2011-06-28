using System.Threading;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport;
using NServiceBus.Config;
using NServiceBus.MasterNode;

namespace NServiceBus.Distributor
{
    public class ReadyMessageManager
    {
        public IManageTheMasterNode masterNodeManager { get; set; }
        public IStartableBus Bus { get; set; }
        public ITransport EndpointTransport { get; set; }
        public IBus EndpointBus { get; set; }

        public int NumberOfWorkerThreads { get; set; }
        private static Address ControlQueue { get; set; }
        
        /// <summary>
        /// Assumes that the bus is already started.
        /// </summary>
        public void Run()
        {
            if (RoutingConfig.IsConfiguredAsMasterNode)
                ControlQueue = Address.Local.SubScope(Configurer.DistributorControlName);
            else
            {
                if (!RoutingConfig.IsDynamicNodeDiscoveryOn)
                {
                    var cfg = Configure.GetConfigSection<UnicastBusConfig>();
                    if (cfg != null && !string.IsNullOrEmpty(cfg.DistributorControlAddress))
                        ControlQueue = Address.Parse(cfg.DistributorControlAddress);
                }
                else
                {
                    if (masterNodeManager.GetMasterNode() != null)
                        ControlQueue = masterNodeManager.GetMasterNode().SubScope(Configurer.DistributorControlName);
                    else
                    {
                        
                    }
                }
            } 

            SendReadyMessage(true);

            EndpointTransport.FinishedMessageProcessing += (a, b) => SendReadyMessage(false);
        }

        public void SendReadyMessage(bool startup)
        {
            if (ControlQueue == null)
                return;

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
