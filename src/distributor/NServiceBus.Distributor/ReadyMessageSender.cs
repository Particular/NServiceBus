namespace NServiceBus.Distributor
{
    using System.Collections.Generic;
    using Unicast.Transport;
    using Config;
    using MasterNode;
    using Unicast.Queuing;

    public class ReadyMessageSender : IWantToRunWhenConfigurationIsComplete
    {
        readonly IManageTheMasterNode masterNodeManager;
        readonly ITransport endpointTransport;
        readonly ISendMessages messageSender;

        public ReadyMessageSender(IManageTheMasterNode masterNodeManager, ITransport endpointTransport, ISendMessages messageSender)
        {
            this.masterNodeManager = masterNodeManager;
            this.endpointTransport = endpointTransport;
            this.messageSender = messageSender;
        }
        
        public void Run()
        {
            var bus = Configure.Instance.Builder.Build<IStartableBus>();
            bus.Started += (obj, ev) => Start();
        }

        void Start()
        {
            if(!ConfigureDistributor.DistributorEnabled) 
                return;

            var masterNodeAddress = masterNodeManager.GetMasterNode();
            
            //hack
            if (RoutingConfig.IsConfiguredAsMasterNode)
                masterNodeAddress = Address.Parse(masterNodeAddress.ToString().Replace(".worker", ""));

            var controlQueue = masterNodeAddress.SubScope(DistributorSetup.DistributorControlName);

            var capacityAvailable = endpointTransport.NumberOfWorkerThreads;
            SendReadyMessage(controlQueue,capacityAvailable,true);

            endpointTransport.FinishedMessageProcessing += (a, b) => SendReadyMessage(controlQueue,1);
        }

        void SendReadyMessage(Address controlQueue,int capacityAvailable,bool isStarting = false)
        {
            var readyMessage = new TransportMessage
                              {
                                  Headers = new Dictionary<string, string>(),
                                  ReplyToAddress = Address.Local
                              };

            readyMessage.Headers.Add(Headers.ControlMessage, true.ToString());
            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable,capacityAvailable.ToString());
            
            if (isStarting)
                readyMessage.Headers.Add(Headers.WorkerStarting, true.ToString());
            

            messageSender.Send(readyMessage, controlQueue);
        }


    }
}
