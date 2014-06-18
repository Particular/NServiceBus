namespace NServiceBus
{
    using Config;
    using Installation;

    /// <summary>
    /// Convenience methods for configuring how instances of  <see cref="INeedToInstallSomething"/>s are run.
    /// </summary>
    public static class InstallConfigExtensions
    {
        /// <summary>
        /// Enable all <see cref="INeedToInstallSomething"/> to run when <see cref="IWantToRunWhenConfigurationIsComplete"/>.
        /// </summary>
        /// <param name="config">The instance of <see cref="Configure"/> to apply these settings to.</param>
        /// <param name="username">The username to pass to <see cref="INeedToInstallSomething.Install"/></param>
        public static Configure EnableInstallers(this Configure config, string username = null)
        {
            if (username != null)
            {
                config.Settings.Set("installation.userName", username);
            }
            config.Features(x => x.Enable<InstallationSupport>());
            return config;
        }
    }
}