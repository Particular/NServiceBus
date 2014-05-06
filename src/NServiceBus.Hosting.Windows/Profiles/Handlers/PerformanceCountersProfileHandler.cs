namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Hosting.Profiles;

    /// <summary>
    /// Handles the PerformanceCounters profile.
    /// </summary>
    public class PerformanceCountersProfileHandler : IHandleProfile<PerformanceCounters>
    {
        public void ProfileActivated()
        {
            Configure.Instance.EnablePerformanceCounters();
        }
    }
}