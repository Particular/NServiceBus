using NServiceBus.Hosting.Profiles;
using NServiceBus.Utils;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
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