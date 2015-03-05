namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using NServiceBus.Installation;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides support for running installers
    /// </summary>
    public class InstallationSupport : Feature
    {
        internal const string UsernameKey = "installation.username";  

        internal InstallationSupport()
        {
            if (Debugger.IsAttached)
            {
                EnableByDefault();
            }
            RegisterStartupTask<InstallerRunner>();
        }

        /// <inheritdoc />
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            foreach (var installerType in GetInstallerTypes(context))
            {
                context.Container.ConfigureComponent(installerType, DependencyLifecycle.InstancePerCall);
            }
        }

        static IEnumerable<Type> GetInstallerTypes(FeatureConfigurationContext context)
        {
            return context.Settings.GetAvailableTypes()
                .Where(t => typeof(INeedToInstallSomething).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface));
        }

        class InstallerRunner : FeatureStartupTask
        {
            IBuilder builder;
            ReadOnlySettings readOnlySettings;
            Configure configure;

            public InstallerRunner(IBuilder builder, ReadOnlySettings readOnlySettings, Configure configure)
            {
                this.builder = builder;
                this.readOnlySettings = readOnlySettings;
                this.configure = configure;
            }

            protected override void OnStart()
            {
                var username = GetInstallationUserName(readOnlySettings);
                foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
                {
                    installer.Install(username, configure);
                }
            }

            static string GetInstallationUserName(ReadOnlySettings settings)
            {
                string username;
                if (settings.TryGet(UsernameKey, out username))
                {
                    return username;
                }

                return WindowsIdentity.GetCurrent().Name;
            }
        }
    }
}