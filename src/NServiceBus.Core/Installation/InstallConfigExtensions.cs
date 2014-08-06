namespace NServiceBus
{
    using Config;
    using Installation;

    /// <summary>
    /// Convenience methods for configuring how instances of  <see cref="INeedToInstallSomething"/>s are run.
    /// </summary>
    public static partial class InstallConfigExtensions
    {
        /// <summary>
        /// Enable all <see cref="INeedToInstallSomething"/> to run when <see cref="IWantToRunWhenConfigurationIsComplete"/>.
        /// </summary>
        /// <param name="config">The instance of <see cref="ConfigurationBuilder"/> to apply these settings to.</param>
        /// <param name="username">The username to pass to <see cref="INeedToInstallSomething.Install"/></param>
        public static ConfigurationBuilder EnableInstallers(this ConfigurationBuilder config, string username = null)
        {
            if (username != null)
            {
                config.settings.Set("installation.userName", username);
            }

            config.settings.Set("installation.enable", true);

            return config;
        }
    }
}