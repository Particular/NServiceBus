namespace NServiceBus.Persistence.Raven.Installation
{
    using System.Security.Principal;
    using NServiceBus.Installation;
    using NServiceBus.Installation.Environments;

    public class RavenDBInstaller : INeedToInstallInfrastructure<Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!InstallEnabled)
                return;
            
            new Setup.Windows.RavenDB.RavenDBSetup().Install(identity,RavenPersistenceConstants.DefaultPort, null,true);  
        }

        static RavenDBInstaller()
        {
            InstallEnabled = true;
        }

        public static bool InstallEnabled { get; set; }     
    }
}