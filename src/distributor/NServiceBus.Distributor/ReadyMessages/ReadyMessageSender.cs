namespace NServiceBus.Distributor.ReadyMessages
{
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Transport;
    using NServiceBus.Unicast.Queuing;
    using log4net;

    public class ReadyMessageSender : IWantToRunWhenTheBusStarts
    {
        public ITransport EndpointTransport { get; set; }
        
        public ISendMessages MessageSender { get; set; }

        public UnicastBus Bus { get; set; }

        public Address DistributorControlAddress { get; set; }

        public void Run()
        {
            if (!Configure.Instance.DistributorEnabled()) 
                return;

            var capacityAvailable = EndpointTransport.NumberOfWorkerThreads;
            SendReadyMessage(capacityAvailable,true);

            EndpointTransport.FinishedMessageProcessing += (a, b) => SendReadyMessage(1);
        }

        void SendReadyMessage(int capacityAvailable,bool isStarting = false)
        {
            CheckForValidLicense();
            
            var readyMessage = ControlMessage.Create();

            readyMessage.ReplyToAddress = Bus.InputAddress; //we use the actual address to make sure that the worker inside the masternode will check in correctly
            
            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable,capacityAvailable.ToString());
            
            if (isStarting)
                readyMessage.Headers.Add(Headers.WorkerStarting, true.ToString());


            MessageSender.Send(readyMessage, DistributorControlAddress);
        }
        private static void CheckForValidLicense()
        {
            if(!Configure.Instance.HasValidLicense() && !IsLocal())
                Logger.Error("In accordance with NServiceBus licensing agreement, when running your worker from a different machine than your Distributor, a valid license should exist on both endpoints. Please go to http://nservicebus.com/License.aspx to obtain a valid license.");
        }

        private static bool? isLocal;
        private static bool IsLocal()
        {
            if (isLocal.HasValue)
                return isLocal.GetValueOrDefault();
            
            var masterNodeName = Configure.Instance.GetMasterNode();
            if (string.IsNullOrWhiteSpace(masterNodeName))
            {
                isLocal = true;
                return true;
            }
            isLocal = ConfigureDistributor.IsLocalIpAddress(masterNodeName);
            return isLocal.GetValueOrDefault();
        }

        static readonly ILog Logger = LogManager.GetLogger("Worker");
    }
}
