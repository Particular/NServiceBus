namespace NServiceBus.Unicast.Queuing.Msmq.Config.Installers
{
    using System.Security.Principal;
    using Installation;
    using Setup.Windows.Msmq;

    public class MsmqInfrastructureInstaller : INeedToInstallInfrastructure<Installation.Environments.Windows>
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

            //allow reinstall to be backwards compatible
            MsmqSetup.StartMsmqIfNecessary(true);
        }
    }
}