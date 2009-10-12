using NServiceBus.Utils;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Installs performance counters.
    /// </summary>
    public class InstallPerformanceCountersProfileHandler : IHandleProfile<InstallPerformanceCounters>
    {
        void IHandleProfile.ProfileActivated()
        {
            PerformanceCounterInstallation.InstallConters();
        }
    }
}
