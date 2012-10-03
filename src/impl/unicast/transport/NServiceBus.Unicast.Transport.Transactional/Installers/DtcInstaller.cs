namespace NServiceBus.Unicast.Transport.Transactional.Installers
{
    using System.Security.Principal;
    using Installation;
    using Installation.Environments;
    using Setup.Windows.Dtc;

    public class DtcInstaller:INeedToInstallInfrastructure<Windows>
    {
        static DtcInstaller()
        {
            IsEnabled = true;
        }

        public static bool IsEnabled { get; set; }

        public void Install(WindowsIdentity identity)
        {
            if(!IsEnabled)
                return;
            
            DtcSetup.StartDtcIfNecessary();
        }
    }
}