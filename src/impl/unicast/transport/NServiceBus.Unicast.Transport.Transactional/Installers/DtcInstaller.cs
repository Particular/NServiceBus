namespace NServiceBus.Unicast.Transport.Transactional.Installers
{
    using System.Security.Principal;
    using Installation;
    using Installation.Environments;
    using Utils;

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
            
            DtcUtil.StartDtcIfNecessary();
        }
    }
}