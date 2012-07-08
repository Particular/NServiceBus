using System.Security.Principal;
using NServiceBus.Unicast.Queuing;

namespace NServiceBus.Proxy
{
    class Installer : IWantQueuesCreated<Installation.Environments.Windows>
    {
        public ICreateQueues Creator { get; set; }

        public void Create(WindowsIdentity identity)
        {
            var s = Configure.Instance.Builder.Build<MsmqProxyDataStorage>();
            Creator.CreateQueueIfNecessary(s.StorageQueue, identity.Name, ConfigureVolatileQueues.IsVolatileQueues);
        }
    }
}
