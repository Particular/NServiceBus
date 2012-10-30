namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Security.Principal;
    using Installation;
    using Installation.Environments;
    using Setup.Windows.PerformanceCounters;

    /// <summary>
    /// Performs installation of the performance counters 
    /// </summary>
    public class PerformanceCounterInstaller : INeedToInstallInfrastructure<Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            Console.WriteLine("Starting installation of PerformanceCounters ");

            PerformanceCounterSetup.SetupCounters(true);

            Console.WriteLine("Installation of PerformanceCounters successful.");
        }
    }
}
