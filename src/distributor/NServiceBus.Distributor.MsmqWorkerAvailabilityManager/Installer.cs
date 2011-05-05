using System.Security.Principal;
using NServiceBus.Installation;
using NServiceBus.Utils;

namespace NServiceBus.Distributor.MsmqWorkerAvailabilityManager
{
    class Installer : INeedToInstallSomething<NServiceBus.Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            var m = NServiceBus.Configure.Instance.Builder.Build<MsmqWorkerAvailabilityManager>();

            MsmqUtilities.CreateQueueIfNecessary(m.StorageQueue, identity.Name);
        }
    }
}
