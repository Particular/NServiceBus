using System;

namespace NServiceBus.Installation.Windows
{
    class DtcInstaller : INeedToInstallInfrastructure<Environments.Windows>
    {
        public void Install()
        {
            Utils.DtcUtil.StartDtcIfNecessary();
        }
    }
}
