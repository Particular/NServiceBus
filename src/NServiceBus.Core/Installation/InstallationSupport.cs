namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Config;
    using Installation;
    using ObjectBuilder;
    using Settings;

    /// <summary>
    /// Provides support for running installers
    /// </summary>
    public class InstallationSupport : Feature
    {
        internal InstallationSupport()
        {
            EnableByDefault();
            
            if (!Debugger.IsAttached)
            {
                Prerequisite(context =>
                {
                    bool enabled;
                    context.Settings.TryGet("installation.enable", out enabled);

                    return enabled;
                }, "EnableInstallers was not invoked.");       
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

            public void Run(Configure config)
            {
                var username = readOnlySettings.GetInstallationUserName();
                foreach (var installer in builder.BuildAll<INeedToInstallSomething>())
                {
                    installer.Install(username, configure);
                }
            }
        }
    }
}