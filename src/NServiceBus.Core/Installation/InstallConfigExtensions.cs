#nullable enable

namespace NServiceBus;

using System;
using Features;
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
    public static void EnableInstallers(this EndpointConfiguration config, string? username = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (username != null)
        {
            config.Settings.Get<InstallerRegistry>().SetUserName(username);
        }

        config.Settings.Set("Installers.Enable", true);
    }

    /// <summary>
    /// Registers the installer type.
    /// </summary>
    public static void RegisterInstaller<TInstaller>(this EndpointConfiguration config) where TInstaller : class, INeedToInstallSomething
    {
        ArgumentNullException.ThrowIfNull(config);

        config.Settings.Get<InstallerRegistry>().Add<TInstaller>();
    }

    /// <summary>
    /// Registers the installer type.
    /// </summary>
    public static void RegisterInstaller<TInstaller>(this FeatureConfigurationContext context) where TInstaller : class, INeedToInstallSomething
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Settings.Get<InstallerRegistry>().Add<TInstaller>();
    }
}