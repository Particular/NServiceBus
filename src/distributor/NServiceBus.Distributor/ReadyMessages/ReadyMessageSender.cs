namespace NServiceBus.Distributor.ReadyMessages
{
    using Unicast;
    using Unicast.Transport;
    using Unicast.Queuing;
    using log4net;

    public class ReadyMessageSender : IWantToRunWhenTheBusStarts
    {
        public ITransport EndpointTransport { get; set; }

        public ISendMessages MessageSender { get; set; }

        public UnicastBus Bus { get; set; }

        public Address DistributorControlAddress { get; set; }

        public void Run()
        {
            if (!Configure.Instance.WorkerRunsOnThisEndpoint())
                return;

            var capacityAvailable = EndpointTransport.NumberOfWorkerThreads;
            SendReadyMessage(capacityAvailable, true);

            EndpointTransport.FinishedMessageProcessing += (a, b) =>
                                                               {
                                                                   if (((IBus)Bus).CurrentMessageContext.Headers.ContainsKey(NServiceBus.Headers.Retries))
                                                                       return;
                                                                       
                                                                   SendReadyMessage(1);
                                                               };
        }

        void SendReadyMessage(int capacityAvailable, bool isStarting = false)
        {
            var readyMessage = ControlMessage.Create();

            readyMessage.ReplyToAddress = Bus.InputAddress; //we use the actual address to make sure that the worker inside the masternode will check in correctly

            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable, capacityAvailable.ToString());

            if (isStarting)
                readyMessage.Headers.Add(Headers.WorkerStarting, true.ToString());


            MessageSender.Send(readyMessage, DistributorControlAddress);
        }
    }
}
