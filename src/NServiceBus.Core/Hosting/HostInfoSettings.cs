namespace NServiceBus
{
    using System;
    using NServiceBus.Hosting;
    using NServiceBus.Utils;

    /// <summary>
    ///     Configuration class for <see cref="HostInformation"/> settings.
    /// </summary>
    public class HostInfoSettings
    {
        BusConfiguration config;

        internal HostInfoSettings(BusConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        /// In this mode, the host id is derived from the installed file path and the current machine name.
        /// </summary>
        /// <remarks>
        /// This mode is only recommended if upgrades are deployed always to the same path.
        /// When using <a href="https://octopusdeploy.com/">Octopus Deploy</a> do not use this mode, instead use <see cref="UsingNames"/>.
        /// </remarks>
        public HostInfoSettings UsingInstalledFilePath()
        {
            //This is the default, we don't need to do anything

            return this;
        }

        /// <summary>
        /// In this mode, the host id is fully managed by the user.
        /// </summary>
        /// <remarks>
        /// This mode is only recommended if you know what you are doing.
        /// The id should be the same across endpoint restarts unless physical host has changed.
        /// </remarks>
        public HostInfoSettings UsingCustomIdentifier(Guid id)
        {
            config.Settings.Set("NServiceBus.HostInformation.HostId", id);
            return this;
        }

        /// <summary>
        /// In this mode, a host id will be generated from <paramref name="instanceName"/> and <paramref name="hostName"/>.
        /// </summary>
        /// <remarks>
        /// This mode is recommended when deploying in Azure roles or <see cref="UsingInstalledFilePath"/> is not appropriate.
        /// </remarks>
        public HostInfoSettings UsingNames(string instanceName, string hostName)
        {
            config.Settings.Set("NServiceBus.HostInformation.HostId", DeterministicGuid.Create(instanceName, hostName));
            return this;
        }

        /// <summary>
        /// Allows to override the display name.
        /// </summary>
        public HostInfoSettings UsingCustomDisplayName(string displayName)
        {
            config.Settings.Set("NServiceBus.HostInformation.DisplayName", displayName);
            return this;
        }
    }
}