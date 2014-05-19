namespace NServiceBus.Startup
{
    using System;
    using Config;
    using Installation;
    using Installation.Environments;
    using Pipeline;
    using Pipeline.Contexts;

    internal class ConfigureContainerTypesBehavior : BaseConfigurationBehavior, IBehavior<ConfigurationContext>
    {
        public void Invoke(ConfigurationContext context, Action next)
        {
            this.context = context;

            ForAllTypes<INeedToInstallSomething<Windows>>(t => context.Configure.Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));
            ForAllTypes<IWantToRunWhenConfigurationIsComplete>(t => context.Configure.Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));
            ForAllTypes<IWantToRunWhenBusStartsAndStops>(t => context.Configure.Configurer.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));
            next();
        }
    }
}