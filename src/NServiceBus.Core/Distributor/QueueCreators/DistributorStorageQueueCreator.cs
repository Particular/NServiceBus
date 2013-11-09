namespace NServiceBus.Transports.Msmq.WorkerAvailabilityManager
{
    using Unicast.Queuing;

    ///<summary>
    /// Signal to create the queue to store worker availability information.
    ///</summary>
    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]    
    public class DistributorStorageQueueCreator : IWantQueueCreated
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
            get { return (!Configure.Instance.Configurer.HasComponent<MsmqWorkerAvailabilityManager>()); }
        }
    }
}
