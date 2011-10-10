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

        public void Run()
        {
            var bus = Configure.Instance.Builder.Build<IStartableBus>();
            bus.Started += (obj, ev) => Start();
        }

        void Start()
        {
            var masterNodeAddress = MasterNodeManager.GetMasterNode();
            
            //hack
            if (RoutingConfig.IsConfiguredAsMasterNode)
                masterNodeAddress = Address.Parse(masterNodeAddress.ToString().Replace(".worker", ""));

            ControlQueue = masterNodeAddress.SubScope(Configurer.DistributorControlName);

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
                messages = new IMessage[] { new ReadyMessage() };
            }

            EndpointBus.Send(ControlQueue, messages);
        }

        static Address ControlQueue { get; set; }

    }
}
