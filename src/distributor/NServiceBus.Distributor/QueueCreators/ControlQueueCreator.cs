namespace NServiceBus.Distributor.QueueCreators
{
    using Unicast.Queuing;
    
    /// <summary>
    /// Signals to create the Distributor control queue
    /// </summary>
    public class ControlQueueCreator : IWantQueueCreated
    {
        public DistributorReadyMessageProcessor DistributorReadyMessageProcessor { get; set; }
        /// <summary>
        /// Address of Distributor control queue
        /// </summary>
        public Address Address
        {
            get { return DistributorReadyMessageProcessor.ControlQueue; }
        }
        
        /// <summary>
        /// Disabling the creation of the Distributor control queue
        /// </summary>
        public bool IsDisabled
        {
            get { return !Configure.Instance.DistributorConfiguredToRunOnThisEndpoint(); }
        }
    }
}
