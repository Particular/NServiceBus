namespace NServiceBus.Distributor
{
    using System;
    using Logging;
    using ReadyMessages;
    using Satellites;
    using Settings;
    using Unicast.Transport;

    class DistributorReadyMessageProcessor : IAdvancedSatellite
    {
        static ILog Logger = LogManager.GetLogger("NServiceBus.Distributor." + Configure.EndpointName);
        static Address Address;
        static bool Disable;

        static DistributorReadyMessageProcessor()
        {
            Address = Configure.Instance.GetMasterNodeAddress().SubScope("distributor.control");
            Disable = !Configure.Instance.DistributorConfiguredToRunOnThisEndpoint() || SettingsHolder.Get<int>("Distributor.Version") != 1;
        }

        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }

        public bool Handle(TransportMessage message)
        {
            if (!message.IsControlMessage())
                return true;

            HandleControlMessage(message);

            return true;
        }

        public Address InputAddress
        {
            get { return Address; }
        }

        public bool Disabled
        {
            get { return Disable; }
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return receiver =>
            {
                //we don't need any DTC for the distributor
                receiver.TransactionSettings.DontUseDistributedTransactions = true;
                receiver.TransactionSettings.DoNotWrapHandlersExecutionInATransactionScope = true;
            };
        }

        void HandleControlMessage(TransportMessage controlMessage)
        {
            var replyToAddress = controlMessage.ReplyToAddress;

            if (controlMessage.Headers.ContainsKey(Headers.WorkerStarting))
            {
                WorkerAvailabilityManager.ClearAvailabilityForWorker(replyToAddress);
                Logger.InfoFormat("Worker {0} has started up, clearing previous reported capacity", replyToAddress);
            }

            string workerCapacityAvailable;
            if (controlMessage.Headers.TryGetValue(Headers.WorkerCapacityAvailable, out workerCapacityAvailable))
            {
                var capacity = int.Parse(workerCapacityAvailable);

                WorkerAvailabilityManager.WorkerAvailable(replyToAddress, capacity);

                Logger.InfoFormat("Worker {0} checked in with available capacity: {1}", replyToAddress, capacity);
            }
        }
    }
}