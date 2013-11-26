namespace NServiceBus.Distributor
{
    using System;
    using Logging;
    using Satellites;
    using Settings;
    using Transports;
    using Unicast.Transport;

    /// <summary>
    ///     Provides functionality for distributing messages from a bus
    ///     to multiple workers when using a unicast transport.
    /// </summary>
    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class DistributorSatellite : IAdvancedSatellite
    {
        static DistributorSatellite()
        {
            Address = Configure.Instance.GetMasterNodeAddress();
            Disable = !Configure.Instance.DistributorConfiguredToRunOnThisEndpoint() || SettingsHolder.Get<int>("Distributor.Version") != 1;
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
        /// The <see cref="Address"/> for this <see cref="ISatellite"/> to use when receiving messages.
        /// </summary>
        public Address InputAddress
        {
            get { return Address; }
        }

        /// <summary>
        /// Set to <code>true</code> to disable this <see cref="ISatellite"/>.
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
            WorkerManager.Start();
        }

        /// <summary>
        ///     Stops the Distributor.
        /// </summary>
        public void Stop()
        {
            WorkerManager.Stop();
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
        /// This method is called when a message is available to be processed.
        /// </summary>
        /// <param name="message">The <see cref="TransportMessage"/> received.</param>
        public bool Handle(TransportMessage message)
        {
            var destination = WorkerManager.PopAvailableWorker();

            if (destination == null)
                return false;

            Logger.Debug("Sending message to: " + destination);
            MessageSender.Send(message, destination);

            return true;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(DistributorSatellite));

        static readonly Address Address;
        static readonly bool Disable;
    }
}