namespace VideoStore.ECommerce.Injection
{
    using System;
    using System.Linq;
    using System.Web.Mvc;
    using Microsoft.AspNet.SignalR;
    using NServiceBus;

    public static class ConfigureDependencyInjection
    {
        public static Configure ModifyMvcAndSignalRToUseOurContainer(this Configure configure)
        {
            // Register our controller activator with NSB
            configure.Configurer.RegisterSingleton(typeof(IControllerActivator),
                new NServiceBusMvcControllerActivator());

            // Find every controller and hub classes so that we can register it
            var typesToRegister = Configure.TypesToScan
                .Where(t => typeof(IController).IsAssignableFrom(t) || typeof(Hub).IsAssignableFrom(t));

            foreach (Type type in typesToRegister)
            {
                configure.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall);
            }

            // Set the SignalR dependency resolver to use our resolver
            GlobalHost.DependencyResolver = new NServiceBusSignalRDependencyResolverAdapter(configure.Builder);

            // Set the MVC dependency resolver to use our resolver
            DependencyResolver.SetResolver(new NServiceBusDependencyResolverAdapter(configure.Builder));

            return configure;
        }
    }
}
