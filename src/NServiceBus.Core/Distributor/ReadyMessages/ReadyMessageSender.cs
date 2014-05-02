namespace NServiceBus.Distributor.ReadyMessages
{
    using System;
    using Settings;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    class ReadyMessageSender : IWantToRunWhenBusStartsAndStops
    {
        public ISendMessages MessageSender { get; set; }

        public UnicastBus Bus { get; set; }

        public Address DistributorControlAddress { get; set; }

        ITransport transport;

        public void Start()
        {
            if (!Configure.Instance.WorkerRunsOnThisEndpoint() || SettingsHolder.Get<int>("Distributor.Version") != 1)
            {
                return;
            }

            transport = Bus.Transport;
            var capacityAvailable = transport.MaximumConcurrencyLevel;
            SendReadyMessage(capacityAvailable, true);

            transport.FinishedMessageProcessing += TransportOnFinishedMessageProcessing;
        }

        public void Stop()
        {
            //transport will be null if !WorkerRunsOnThisEndpoint
            if (transport != null)
            {
                transport.FinishedMessageProcessing -= TransportOnFinishedMessageProcessing;
            }
        }

        void TransportOnFinishedMessageProcessing(object sender, FinishedMessageProcessingEventArgs eventArgs)
        {
            //if there was a failure this "send" will be rolled back
            SendReadyMessage();
        }

        void SendReadyMessage(int capacityAvailable = 1, bool isStarting = false)
        {
            //we use the actual address to make sure that the worker inside the master node will check in correctly
            var readyMessage = ControlMessage.Create(Bus.InputAddress);

            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable, capacityAvailable.ToString());

            if (isStarting)
                readyMessage.Headers.Add(Headers.WorkerStarting, Boolean.TrueString);

            MessageSender.Send(readyMessage, DistributorControlAddress);
        }
    }
}
