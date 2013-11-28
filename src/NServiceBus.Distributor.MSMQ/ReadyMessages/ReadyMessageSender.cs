namespace NServiceBus.Distributor.MSMQ.ReadyMessages
{
    using System;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    internal class ReadyMessageSender : IWantToRunWhenBusStartsAndStops
    {
        public ISendMessages MessageSender { get; set; }

        public UnicastBus Bus { get; set; }

        public Address DistributorControlAddress { get; set; }

        public void Start()
        {
            if (!ConfigureMSMQDistributor.WorkerRunsOnThisEndpoint())
            {
                return;
            }

            transport = Bus.Transport;
            var capacityAvailable = transport.MaximumConcurrencyLevel;
            SendReadyMessage(workerSessionId, capacityAvailable, true);

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

        void TransportOnFinishedMessageProcessing(object sender, FinishedMessageProcessingEventArgs e)
        {
            //if there was a failure this "send" will be rolled back
            string messageSessionId;
            e.Message.Headers.TryGetValue(Headers.WorkerSessionId, out messageSessionId);

            //If the message we are processing contains an old sessionid then we do not send an extra control message 
            //otherwise that would cause https://github.com/Particular/NServiceBus/issues/978
            if (messageSessionId == workerSessionId)
            {
                SendReadyMessage(messageSessionId);
            }
        }

        void SendReadyMessage(string sessionId, int capacityAvailable = 1, bool isStarting = false)
        {
            //we use the actual address to make sure that the worker inside the master node will check in correctly
            var readyMessage = ControlMessage.Create(Bus.InputAddress);

            readyMessage.Headers.Add(Headers.WorkerCapacityAvailable, capacityAvailable.ToString());
            readyMessage.Headers.Add(Headers.WorkerSessionId, sessionId);

            if (isStarting)
            {
                readyMessage.Headers.Add(Headers.WorkerStarting, Boolean.TrueString);
            }

            MessageSender.Send(readyMessage, DistributorControlAddress);
        }

        ITransport transport;
        string workerSessionId = Guid.NewGuid().ToString();
    }
}