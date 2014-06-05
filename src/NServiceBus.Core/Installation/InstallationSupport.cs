namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using Config;
    using Features;
    using Installation;
    using ObjectBuilder;
    using Settings;

    public class InstallationSupport : Feature
    {
        internal InstallationSupport()
        {
            if (Debugger.IsAttached)
            {
                EnableByDefault();
            }
            //TODO:
            //  RegisterStartupTask<Starter>();
        }

        protected override void Setup(FeatureConfigurationContext context)
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

            string GetUsername()
            {
                var username = readOnlySettings.GetOrDefault<string>("installation.userName");
                if (username == null)
                {
                    return WindowsIdentity.GetCurrent().Name;
                }
                return username;
            }

            public void Run(Configure config)
            {
                var username = GetUsername();
                foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
                {
                    installer.Install(username, configure);
                }
            }
        }
    }

}