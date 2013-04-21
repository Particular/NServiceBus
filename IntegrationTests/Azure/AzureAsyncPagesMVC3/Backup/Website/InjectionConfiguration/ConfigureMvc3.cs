using System;
using System.Linq;
using System.Web.Mvc;
using NServiceBus;

namespace Website
{
    public static class ConfigureMvc3
    {
        public static Configure ForMvc(this Configure configure)
        {
            // Register our controller activator with NSB
            configure.Configurer.RegisterSingleton(typeof(IControllerActivator),
                new NServiceBusControllerActivator());

            // Find every controller class so that we can register it
            var controllers = Configure.TypesToScan
                .Where(t => typeof(IController).IsAssignableFrom(t));

            // Register each controller class with the NServiceBus container
            foreach (Type type in controllers)
                configure.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall);

            // Set the MVC dependency resolver to use our resolver
            DependencyResolver.SetResolver(new NServiceBusDependencyResolverAdapter(configure.Builder));

            // Required by the fluent configuration semantics
            return configure;
        }
    }
}