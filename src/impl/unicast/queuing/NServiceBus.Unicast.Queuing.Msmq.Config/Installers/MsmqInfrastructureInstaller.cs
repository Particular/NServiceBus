namespace NServiceBus.Unicast.Queuing.Msmq.Config.Installers
{
    using System.Security.Principal;
    using Installation;
    using Utils;

    class MsmqInfrastructureInstaller : INeedToInstallInfrastructure<Installation.Environments.Windows>,IWantToRunBeforeConfiguration
    {
        static MsmqInfrastructureInstaller()
        {
            Enabled = true; //enabled when run stand alone
        }

        public static bool Enabled { get; set; }

        public void Install(WindowsIdentity identity)
        {
            if (!Enabled)
                return;

            MsmqInstallation.StartMsmqIfNecessary();
        }

        public void Init()
        {
            Enabled = false; //disabled by default when running in a endpoint
        }
    }
}