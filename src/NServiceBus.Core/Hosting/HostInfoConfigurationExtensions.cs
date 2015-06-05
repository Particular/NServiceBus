namespace NServiceBus
{
    /// <summary>
    /// Extension methods to configure hostid.
    /// </summary>
    public static class HostInfoConfigurationExtensions
    {
        /// <summary>
        /// Entry point for HostInfo related configuration
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static HostInfoSettings UniquelyIdentifyRunningInstance(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            return new HostInfoSettings(config);
        }
    }
}