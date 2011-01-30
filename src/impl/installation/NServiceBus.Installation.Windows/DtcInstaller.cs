using System;
using System.Security.Principal;

namespace NServiceBus.Installation.Windows
{
    class DtcInstaller : INeedToInstallInfrastructure<Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            Utils.DtcUtil.StartDtcIfNecessary();
        }
    }
}
