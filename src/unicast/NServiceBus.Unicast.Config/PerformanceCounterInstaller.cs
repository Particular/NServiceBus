namespace NServiceBus.Unicast.Config
{
    using System.Security.Principal;
    using Installation;
    using Installation.Environments;
    using Utils;


    /// <summary>
    /// Performs installation of the performance counters 
    /// </summary>
    public class PerformanceCounterInstaller:INeedToInstallInfrastructure<Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            PerformanceCounterInstallation.InstallCounters();            
        }
    }
}