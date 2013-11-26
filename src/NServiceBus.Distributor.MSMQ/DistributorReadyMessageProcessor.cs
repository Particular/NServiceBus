namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using ReadyMessages;
    using Satellites;
    using Unicast.Transport;

    /// <summary>
    ///     Part of the Distributor infrastructure.
    /// </summary>
    internal class DistributorReadyMessageProcessor : IAdvancedSatellite
    {
        static DistributorReadyMessageProcessor()
        {
            Address = Configure.Instance.GetMasterNodeAddress().SubScope("distributor.control");
            Disable = !ConfigureMSMQDistributor.DistributorConfiguredToRunOnThisEndpoint();
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
            {
                return true;
            }

            if (message.Headers.ContainsKey(Headers.UnregisterWorker))
            {
                HandleDisconnectMessage(message);
                return true;
            }

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

        void HandleDisconnectMessage(TransportMessage controlMessage)
        {
            var workerAddress = Address.Parse(controlMessage.Headers[Headers.UnregisterWorker]);

            WorkerAvailabilityManager.UnregisterWorker(workerAddress);
        }

        void HandleControlMessage(TransportMessage controlMessage)
        {
            var replyToAddress = controlMessage.ReplyToAddress;

            if (LicenseConfig.LimitNumberOfWorkers(replyToAddress))
            {
                return;
            }

            string messageSessionId;
            if (!controlMessage.Headers.TryGetValue(Headers.WorkerSessionId, out messageSessionId))
            {
                messageSessionId = String.Empty;
            }

            if (controlMessage.Headers.ContainsKey(Headers.WorkerStarting))
            {
                var capacity = int.Parse(controlMessage.Headers[Headers.WorkerCapacityAvailable]);

                WorkerAvailabilityManager.RegisterNewWorker(new Worker(replyToAddress, messageSessionId), capacity);

                return;
            }

            WorkerAvailabilityManager.WorkerAvailable(new Worker(replyToAddress, messageSessionId));
        }

        static readonly Address Address;
        static readonly bool Disable;
    }
}