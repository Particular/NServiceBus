using System;

namespace NServiceBus.Installation.Windows
{
    class MsmqInstaller : INeedToInstallInfrastructure<Environments.Windows>
    {
        public void Install()
        {
            Utils.MsmqInstallation.StartMsmqIfNecessary();
        }
    }
}
