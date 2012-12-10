namespace NServiceBus.Distributor.ReadyMessages
{
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class ReadyMessageSender : IWantToRunWhenBusStartsAndStops
    {
        public ISendMessages MessageSender { get; set; }

        public UnicastBus Bus { get; set; }

        public Address DistributorControlAddress { get; set; }

        public void Start()
        {
            if (!Configure.Instance.WorkerRunsOnThisEndpoint())
                return;

            var capacityAvailable = Bus.Transport.MaximumConcurrencyLevel;
            SendReadyMessage(capacityAvailable, true);

            Bus.Transport.FinishedMessageProcessing += (a, b) =>
                                                               {
                                                                   if (((IBus)Bus).CurrentMessageContext.Headers.ContainsKey(NServiceBus.Headers.Retries))
                                                                       return;
                                                                       
                                                                   SendReadyMessage(1);
                                                               };
        }

        public void Stop()
        {
            //TODO: Need to add code here
        }

        void SendReadyMessage(int capacityAvailable, bool isStarting = false)
        {
            //we use the actual address to make sure that the worker inside the masternode will check in correctly
            var readyMessage = ControlMessage.Create(Bus.InputAddress);

            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable, capacityAvailable.ToString());

            if (isStarting)
                readyMessage.Headers.Add(Headers.WorkerStarting, true.ToString());

            MessageSender.Send(readyMessage, DistributorControlAddress);
        }
    }
}
