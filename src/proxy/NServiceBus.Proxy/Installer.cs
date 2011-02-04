using System.Security.Principal;
using NServiceBus.Installation;

namespace NServiceBus.Proxy
{
    class Installer : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            var s = Configure.Instance.Builder.Build<MsmqProxyDataStorage>();
            Utils.MsmqUtilities.CreateQueueIfNecessary(s.StorageQueue, identity.Name);
        }
    }
}
