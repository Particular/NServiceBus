namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using NServiceBus.Installation;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides support for running installers.
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
                .Where(t => typeof(IInstall).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface));
        }

        class InstallerRunner : FeatureStartupTask
        {
            IBuilder builder;
            ReadOnlySettings readOnlySettings;

            public InstallerRunner(IBuilder builder, ReadOnlySettings readOnlySettings)
            {
                this.builder = builder;
                this.readOnlySettings = readOnlySettings;
            }

            protected override async Task OnStart(IBusContext context)
            {
                var username = GetInstallationUserName(readOnlySettings);
                foreach (var installer in builder.BuildAll<IInstall>())
                {
                    await installer.Install(username).ConfigureAwait(false);
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