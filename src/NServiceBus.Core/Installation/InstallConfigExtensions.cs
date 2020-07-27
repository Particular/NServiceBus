namespace NServiceBus
{
    using Installation;

    /// <summary>
    /// Convenience methods for configuring how instances of  <see cref="INeedToInstallSomething" />s are run.
    /// </summary>
    public static class InstallConfigExtensions
    {
        /// <summary>
        /// Enable all <see cref="INeedToInstallSomething" /> to run when the configuration is complete.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="username">The username to pass to <see cref="INeedToInstallSomething.Install" />.</param>
        public static void EnableInstallers(this EndpointConfiguration config, string username = null)
        {
            Guard.AgainstNull(nameof(config), config);
            if (username != null)
            {
                config.Settings.Set("Installers.UserName", username);
            }

            config.Settings.Set("Installers.Enable", true);
        }
    }
}