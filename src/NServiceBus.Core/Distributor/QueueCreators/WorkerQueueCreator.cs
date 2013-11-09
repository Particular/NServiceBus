namespace NServiceBus.Distributor.QueueCreators
{
    using Unicast.Queuing;

    /// <summary>
    /// Signal to create the queue for a worker
    /// </summary>
    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]    
    public class WorkerQueueCreator : IWantQueueCreated
    {
        /// <summary>
        /// Address of worker queue
        /// </summary>
        public Address Address
        {
            get { return Address.Local.SubScope("Worker"); }
        }
        /// <summary>
        /// Disabling the creation of the worker queue
        /// </summary>
        public bool IsDisabled
        {
            get {return !((Configure.Instance.DistributorConfiguredToRunOnThisEndpoint()) && (Configure.Instance.WorkerRunsOnThisEndpoint()));}
        }
    }
}
