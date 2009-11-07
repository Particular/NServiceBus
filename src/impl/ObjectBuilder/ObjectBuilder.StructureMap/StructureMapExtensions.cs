using System;
using StructureMap;
using StructureMap.Graph;

namespace NServiceBus.ObjectBuilder.StructureMap
{
    /// <summary>
    /// Extensions to the structuremap api
    /// </summary>
    public static class StructureMapExtensions
    {
        /// <summary>
        /// Registers the given interface and redirects to the given pluginType when the interface is requested
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="implementedInterface"></param>
        /// <param name="pluginType"></param>
        public static void RegisterAdditionalInterfaceForPluginType(this ConfigurationExpression configuration,Type implementedInterface, Type pluginType)
        {
            var type = typeof(Registration<,>).MakeGenericType(implementedInterface, pluginType);

            var registration = (IRegistration)Activator.CreateInstance(type);

            registration.RegisterServiceInterface(configuration);
        }

        /// <summary>
        /// Tells structurmap to do setter injection for the given type
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="pluginType"></param>
        public static void EnableSetterInjectionFor(this ConfigurationExpression configuration, Type pluginType)
        {
            PluginCache.AddFilledType(pluginType);
        }

        // The inner type and interface is just a little trick to
        // grease the generic wheels
        interface IRegistration
        {
            void RegisterServiceInterface(ConfigurationExpression config);
        }

        class Registration<TInterface, TImplementor> : IRegistration where TImplementor : TInterface
        {
            public void RegisterServiceInterface(ConfigurationExpression config)
            {
                config.For<TInterface>().TheDefault.Is.ConstructedBy(ctx => ctx.GetInstance<TImplementor>());
            }
        }

    }
}