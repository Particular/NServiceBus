namespace NServiceBus.Distributor.Installers
{
    using System.Security.Principal;
    using Unicast.Queuing;

    public class WorkerInputInstaller : IWantQueuesCreated<Installation.Environments.Windows>
    {
        public ICreateQueues Creator { get; set; }

        /// <summary>
        /// Install queue for Worker message handler. Will not install if Distributor is not configured to run on this endpoint or if the worker should not run on this endpoint.
        /// </summary>
        /// <param name="identity"></param>
        public void Create(WindowsIdentity identity)
        {
            if ((!Configure.Instance.DistributorConfiguredToRunOnThisEndpoint()) || (!Configure.Instance.WorkerRunsOnThisEndpoint()))
                return;

            //create the worker queue
            Creator.CreateQueueIfNecessary(Address.Local.SubScope("Worker"), identity.Name, ConfigureVolatileQueues.IsVolatileQueues);
        }
    }
}
