using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport;
using NServiceBus.Config;
using NServiceBus.MasterNode;

namespace NServiceBus.Distributor
{
    public class ReadyMessageManager : IWantToRunWhenConfigurationIsComplete
    {
        public IManageTheMasterNode MasterNodeManager { get; set; }
        public ITransport EndpointTransport { get; set; }
        public IBus EndpointBus { get; set; }

        public int NumberOfWorkerThreads { get; set; }
        private static Address ControlQueue { get; set; }

        public void Run()
        {
            var bus = Configure.Instance.Builder.Build<IStartableBus>();
            bus.Started += (obj, ev) => Start();
        }
        
        void Start()
        {
            if (RoutingConfig.IsConfiguredAsMasterNode)
            {
                //todo, check with udi if the distributor really should do any work
                //ControlQueue = Address.Local.SubScope(Configurer.DistributorControlName);

                return;
            }
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
                    if (MasterNodeManager.GetMasterNode() != null)
                        ControlQueue = MasterNodeManager.GetMasterNode().SubScope(Configurer.DistributorControlName);
                }
            } 

            SendReadyMessage(true);

            EndpointTransport.FinishedMessageProcessing += (a, b) => SendReadyMessage(false);
        }

        void SendReadyMessage(bool startup)
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
