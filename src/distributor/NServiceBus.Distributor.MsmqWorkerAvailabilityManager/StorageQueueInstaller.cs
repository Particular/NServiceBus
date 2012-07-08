using System.Security.Principal;

namespace NServiceBus.Distributor.MsmqWorkerAvailabilityManager
{
    using Unicast.Queuing;

    ///<summary>
    /// Creates the queue to store worker availability information.
    ///</summary>
    public class StorageQueueInstaller : IWantQueuesCreated<Installation.Environments.Windows>
    {
        public ICreateQueues Creator { get; set; }

        /// <summary>
        /// Implementation of INeedToInstallSomething.Install
        /// </summary>
        /// <param name="identity"></param>
        public void Create(WindowsIdentity identity)
        {
            if (!Configure.Instance.Configurer.HasComponent<MsmqWorkerAvailabilityManager>())
                return;

            var m = Configure.Instance.Builder.Build<MsmqWorkerAvailabilityManager>();

            Creator.CreateQueueIfNecessary(m.StorageQueueAddress, identity.Name, ConfigureVolatileQueues.IsVolatileQueues);
        }
    }
}
