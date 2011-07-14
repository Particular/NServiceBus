using System;
using System.Security.Principal;

namespace NServiceBus.Installation.Windows
{
    class MsmqInstaller : INeedToInstallInfrastructure<Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            Utils.MsmqInstallation.StartMsmqIfNecessary();
        }
    }
}
