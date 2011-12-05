namespace NServiceBus.Distributor
{
    using System.Collections.Generic;
    using Unicast;
    using Unicast.Transport;
    using MasterNode;
    using Unicast.Queuing;

    public class ReadyMessageSender : IWantToRunWhenTheBusStarts
    {
        public IManageTheMasterNode MasterNodeManager { get; set; }
        public ITransport EndpointTransport { get; set; }
        public ISendMessages MessageSender { get; set; }
        public Address ReturnAddress { get; set; }

        public void Run()
        {
            if(!ConfigureDistributor.DistributorEnabled) 
                return;

            var masterNodeAddress = MasterNodeManager.GetMasterNode();
            
            var controlQueue = masterNodeAddress.SubScope(ConfigureDistributor.DistributorControlName);

            var capacityAvailable = EndpointTransport.NumberOfWorkerThreads;
            SendReadyMessage(controlQueue,capacityAvailable,true);

            EndpointTransport.FinishedMessageProcessing += (a, b) => SendReadyMessage(controlQueue,1);
        }

        void SendReadyMessage(Address controlQueue,int capacityAvailable,bool isStarting = false)
        {
            var readyMessage = new TransportMessage
                              {
                                  Headers = new Dictionary<string, string>(),
                                  ReplyToAddress = ReturnAddress ?? Address.Local
                              };

            readyMessage.Headers.Add(Headers.ControlMessage, true.ToString());
            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable,capacityAvailable.ToString());
            
            if (isStarting)
                readyMessage.Headers.Add(Headers.WorkerStarting, true.ToString());
            

            MessageSender.Send(readyMessage, controlQueue);
        }


    }
}
