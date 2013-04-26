namespace NServiceBus.Distributor.ReadyMessages
{
    using System;
    using Transports;
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

            Bus.Transport.FinishedMessageProcessing += TransportOnFinishedMessageProcessing;
        }

        public void Stop()
        {
            Bus.Transport.FinishedMessageProcessing -= TransportOnFinishedMessageProcessing;
        }

        void TransportOnFinishedMessageProcessing(object sender, EventArgs eventArgs)
        {
            //if there was a failure this "send" will be rolled back
            SendReadyMessage();
        }

        void SendReadyMessage(int capacityAvailable = 1, bool isStarting = false)
        {
            //we use the actual address to make sure that the worker inside the masternode will check in correctly
            var readyMessage = ControlMessage.Create(Bus.InputAddress);

            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable, capacityAvailable.ToString());

            if (isStarting)
                readyMessage.Headers.Add(Headers.WorkerStarting, Boolean.TrueString);

            MessageSender.Send(readyMessage, DistributorControlAddress);
        }
    }
}
