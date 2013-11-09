namespace NServiceBus.Distributor.MSMQ.QueueCreators
{
    using Unicast.Queuing;

    /// <summary>
    ///     Signal to create the queue to store worker availability information.
    /// </summary>
    internal class DistributorStorageQueueCreator : IWantQueueCreated
    {
        /// <summary>
        ///     Holds storage queue address.
        /// </summary>
        public DistributorStorageQueueCreator()
        {
            disabled = !Configure.Instance.Configurer.HasComponent<MsmqWorkerAvailabilityManager>();

            if (disabled)
            {
                return;
            }

            address = Address.Local.SubScope("distributor.storage");
        }

        /// <summary>
        ///     Address of Distributor storage queue.
        /// </summary>
        public Address Address
        {
            get { return address; }
        }

        /// <summary>
        ///     Disabling the creation of the distributor storage queue
        /// </summary>
        public bool IsDisabled
        {
            get { return disabled; }
        }

        Address address;
        bool disabled;
    }
}