namespace NServiceBus
{
    using Installation;
    using NServiceBus.Features;

    /// <summary>
    /// Convenience methods for configuring how instances of  <see cref="INeedToInstallSomething"/>s are run.
    /// </summary>
    public static class InstallConfigExtensions
    {
        /// <summary>
        /// Enable all <see cref="INeedToInstallSomething"/> to run when the configuration is complete
        /// </summary>
        /// <param name="config">The instance of <see cref="BusConfiguration"/> to apply these settings to.</param>
        /// <param name="username">The username to pass to <see cref="INeedToInstallSomething.Install"/></param>
        public static void EnableInstallers(this BusConfiguration config, string username = null)
        {
            Guard.AgainstNull(config, "config");
            if (username != null)
            {
                config.Settings.Set(InstallationSupport.UsernameKey, username);
            }

            config.EnableFeature<InstallationSupport>();
        }
    }
}