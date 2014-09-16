namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using Config;
    using Installation;
    using ObjectBuilder;
    using Settings;

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
        }

        /// <summary>
        /// Invoked if the feature is activated
        /// </summary>
        /// <param name="context">The feature context</param>
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

        class Starter : IWantToRunWhenConfigurationIsComplete
        {
            IBuilder builder;
            ReadOnlySettings readOnlySettings;
            Configure configure;

            public Starter(IBuilder builder, ReadOnlySettings readOnlySettings, Configure configure)
            {
                this.builder = builder;
                this.readOnlySettings = readOnlySettings;
                this.configure = configure;
            }

            string GetInstallationUserName(ReadOnlySettings settings)
            {
                string username;
                if (settings.TryGet(UsernameKey, out username))
                {
                    return username;
                }

                return WindowsIdentity.GetCurrent().Name;
            }

            public void Run(Configure config)
            {
                var username = GetInstallationUserName(readOnlySettings);
                foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
                {
                    installer.Install(username, configure);
                }
            }
        }
    }
}