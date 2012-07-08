using System.Security.Principal;
using NServiceBus.Unicast.Queuing;

namespace NServiceBus.Faults.Forwarder.Config
{
    class Installer : IWantQueuesCreated<Installation.Environments.Windows>
    {
        public ICreateQueues QueueCreator { get; set; }

        public void Create(WindowsIdentity identity)
        {
            if (ConfigureFaultsForwarder.ErrorQueue != null)
                QueueCreator.CreateQueueIfNecessary(ConfigureFaultsForwarder.ErrorQueue, identity.Name, ConfigureVolatileQueues.IsVolatileQueues);            
        }
    }
}
