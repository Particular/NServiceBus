namespace NServiceBus.Transports.Msmq.WorkerAvailabilityManager
{
    using Settings;
    using Unicast.Queuing;

    ///<summary>
    /// Signal to create the queue to store worker availability information.
    ///</summary>
    class DistributorStorageQueueCreator : IWantQueueCreated
    {
        /// <summary>
        /// Holds storage queue address.
        /// </summary>
        public MsmqWorkerAvailabilityManager MsmqWorkerAvailabilityManager { get; set; }

        /// <summary>
        /// Address of Distributor storage queue.
        /// </summary>
        public Address Address
        {
            get {
                return MsmqWorkerAvailabilityManager == null ? null : MsmqWorkerAvailabilityManager.StorageQueueAddress;
            }
        }
        /// <summary>
        /// Disabling the creation of the distributor storage queue
        /// </summary>
        public bool IsDisabled
        {
            get { return !Configure.Instance.Configurer.HasComponent<MsmqWorkerAvailabilityManager>() 
                || SettingsHolder.Get<int>("Distributor.Version") != 1; }
        }
    }
}
