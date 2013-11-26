namespace NServiceBus.Distributor.MSMQ
{
    using System;
    using Logging;
    using ReadyMessages;
    using Satellites;
    using Transports;
    using Unicast.Transport;

    /// <summary>
    ///     Provides functionality for distributing messages from a bus
    ///     to multiple workers when using a unicast transport.
    /// </summary>
    internal class DistributorSatellite : IAdvancedSatellite
    {
        static DistributorSatellite()
        {
            Address = Configure.Instance.GetMasterNodeAddress();
            Disable = !ConfigureMSMQDistributor.DistributorConfiguredToRunOnThisEndpoint();
        }

        /// <summary>
        ///     Object used to send messages.
        /// </summary>
        public ISendMessages MessageSender { get; set; }

        /// <summary>
        ///     Sets the <see cref="IWorkerAvailabilityManager" /> implementation that will be
        ///     used to determine whether or not a worker is available.
        /// </summary>
        public IWorkerAvailabilityManager WorkerManager { get; set; }

        /// <summary>
        ///     The <see cref="Address" /> for this <see cref="ISatellite" /> to use when receiving messages.
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
        ///     Starts the Distributor.
        /// </summary>
        public void Start()
        {
        }

        /// <summary>
        ///     Stops the Distributor.
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

        /// <summary>
        ///     This method is called when a message is available to be processed.
        /// </summary>
        /// <param name="message">The <see cref="TransportMessage" /> received.</param>
        public bool Handle(TransportMessage message)
        {
            var worker = WorkerManager.NextAvailableWorker();

            if (worker == null)
            {
                return false;
            }

            Logger.DebugFormat("Forwarding message to '{0}'.", worker.Address);

            message.Headers[Headers.WorkerSessionId] = worker.SessionId;

            MessageSender.Send(message, worker.Address);

            return true;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(DistributorSatellite));

        static readonly Address Address;
        static readonly bool Disable;
    }
}