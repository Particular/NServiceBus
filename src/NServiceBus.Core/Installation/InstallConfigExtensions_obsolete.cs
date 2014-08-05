namespace NServiceBus
{
    using System;
    using NServiceBus.Config;
    using NServiceBus.Installation;

    /// <summary>
    /// Convenience methods for configuring how instances of  <see cref="INeedToInstallSomething"/>s are run.
    /// </summary>
    public static class InstallConfigExtensions_obsolete
    {
        /// <summary>
        /// Enable all <see cref="INeedToInstallSomething"/> to run when <see cref="IWantToRunWhenConfigurationIsComplete"/>.
        /// </summary>
        /// <param name="config">The instance of <see cref="Configure"/> to apply these settings to.</param>
        /// <param name="username">The username to pass to <see cref="INeedToInstallSomething.Install"/></param>
        [ObsoleteEx(Replacement = "Configure.With(c=>.EnableInstallers())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
// ReSharper disable UnusedParameter.Global
        public static Configure EnableInstallers(this Configure config, string username = null)
// ReSharper restore UnusedParameter.Global
        {
            throw new InvalidOperationException();
        }
    }
}