namespace NServiceBus.Distributor
{
    using System;
    using Logging;
    using ReadyMessages;
    using Satellites;
    using Settings;
    using Unicast.Transport;

    /// <summary>
    ///     Part of the Distributor infrastructure.
    /// </summary>
    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class DistributorReadyMessageProcessor : IAdvancedSatellite
    {
        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Distributor." + Configure.EndpointName);
        private static readonly Address Address;
        private static readonly bool Disable;

        static DistributorReadyMessageProcessor()
        {
            Address = Configure.Instance.GetMasterNodeAddress().SubScope("distributor.control");
            Disable = !Configure.Instance.DistributorConfiguredToRunOnThisEndpoint() || SettingsHolder.Get<int>("Distributor.Version") != 1;
        }

        /// <summary>
        ///     Sets the <see cref="IWorkerAvailabilityManager" /> implementation that will be
        ///     used to determine whether or not a worker is available.
        /// </summary>
        public IWorkerAvailabilityManager WorkerAvailabilityManager { get; set; }

        /// <summary>
        ///     This method is called when a message is available to be processed.
        /// </summary>
        /// <param name="message">
        ///     The <see cref="TransportMessage" /> received.
        /// </param>
        public bool Handle(TransportMessage message)
        {
            if (!message.IsControlMessage())
                return true;

            HandleControlMessage(message);

            return true;
        }

        /// <summary>
        ///     The <see cref="NServiceBus.Address" /> for this <see cref="ISatellite" /> to use when receiving messages.
        /// </summary>
        public Address InputAddress
        {
            get { return Address; }
        }

        /// <summary>
        ///     Set to <code>true</code> to disable this <see cref="ISatellite" />.
        /// </summary>
        public bool Disabled
        {
            get { return Disable; }
        }

        /// <summary>
        ///     Starts the <see cref="ISatellite" />.
        /// </summary>
        public void Start()
        {
        }

        /// <summary>
        ///     Stops the <see cref="ISatellite" />.
        /// </summary>
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

        private void HandleControlMessage(TransportMessage controlMessage)
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