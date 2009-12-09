using NServiceBus.Hosting.Profiles;
using NServiceBus.Utils;

namespace NServiceBus.Host.Profiles.Handlers
{
    /// <summary>
    /// Installs performance counters.
    /// </summary>
    public class InstallPerformanceCountersProfileHandler : IHandleProfile<InstallPerformanceCounters>
    {
        void IHandleProfile.ProfileActivated()
        {
            PerformanceCounterInstallation.InstallCounters();
        }
    }
}