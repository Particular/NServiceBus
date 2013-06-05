namespace NServiceBus.Unicast.Queuing.Msmq.Config.Installers
{
    using System;
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

            if(!MsmqSetup.StartMsmqIfNecessary(false))
                throw new Exception("Failed to setup MSMQ since it needs to be reinstalled. A reinstall will remove any local queues. Please go to http://particular.net/articles/running-nservicebus-on-windows for instructions on how to remedy the situation");
        }
    }
}