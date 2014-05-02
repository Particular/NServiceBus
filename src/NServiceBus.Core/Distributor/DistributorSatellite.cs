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
    class DistributorSatellite : IAdvancedSatellite
    {
        static DistributorSatellite()
        {
            Address = Configure.Instance.GetMasterNodeAddress();
            Disable = !Configure.Instance.DistributorConfiguredToRunOnThisEndpoint() || SettingsHolder.Get<int>("Distributor.Version") != 1;
        }

        public ISendMessages MessageSender { get; set; }

        public IWorkerAvailabilityManager WorkerManager { get; set; }

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
            WorkerManager.Start();
        }

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

        static ILog Logger = LogManager.GetLogger(typeof(DistributorSatellite));

        static Address Address;
        static bool Disable;
    }
}