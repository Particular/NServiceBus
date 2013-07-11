using System.Linq;
using System.Web.Mvc;
using NServiceBus;
using NServiceBus.ObjectBuilder;

namespace Host
{
    public static class ConfigureMvc
    {
        public static Configure ForMvc(this Configure configure)
        {
            configure.Configurer.RegisterSingleton(typeof (IControllerActivator), new NServiceBusControllerActivator());
            configure.Configurer.RegisterSingleton(typeof (IControllerFactory), new DefaultControllerFactory());
            configure.Configurer.RegisterSingleton(typeof (IViewPageActivator), new BasicViewPageActivator());
            configure.Configurer.RegisterSingleton(typeof(ModelMetadataProvider), ModelMetadataProviders.Current);
            
            var controllers = Configure.TypesToScan
                .Where(t => typeof(IController).IsAssignableFrom(t));

            foreach (var type in controllers)
                configure.Configurer.ConfigureComponent(type, DependencyLifecycle.InstancePerCall);

            DependencyResolver.SetResolver(new NServiceBusResolverAdapter(configure.Builder));

            return configure;
        }
    }
}