namespace NServiceBus
{
    using System;

    /// <summary>
    /// Extension methods to configure hostid.
    /// </summary>
    public static class HostInfoConfigurationExtensions
    {
        /// <summary>
        /// Entry point for HostInfo related configuration.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static HostInfoSettings UniquelyIdentifyRunningInstance(this EndpointConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return new HostInfoSettings(config);
        }
    }
}