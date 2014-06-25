namespace NServiceBus.ObjectBuilder.StructureMap
{
    using System;
    using global::StructureMap;
    using global::StructureMap.Graph;
    using global::StructureMap.Pipeline;

    /// <summary>
    /// Extensions to the StructureMap api
    /// </summary>
    public static class StructureMapExtensions
    {
        /// <summary>
        /// Registers the given interface and redirects to the given pluginType when the interface is requested
        /// </summary>
        public static void RegisterAdditionalInterfaceForPluginType(this ConfigurationExpression configuration, Type implementedInterface, Type pluginType, ILifecycle lifecycle)
        {
            var type = typeof(Registration<,>).MakeGenericType(implementedInterface, pluginType);

            var registration = (IRegistration)Activator.CreateInstance(type);

            registration.RegisterServiceInterface(configuration,lifecycle);
        }

        /// <summary>
        /// Tells StructureMap to do setter injection for the given type
        /// </summary>
        public static void EnableSetterInjectionFor(Type pluginType)
        {
            PluginCache.AddFilledType(pluginType);
        }

        // The inner type and interface is just a little trick to
        // grease the generic wheels
        interface IRegistration
        {
            void RegisterServiceInterface(ConfigurationExpression config, ILifecycle callModel);
        }

        class Registration<TInterface, TImplementor> : IRegistration where TImplementor : TInterface
        {
            public void RegisterServiceInterface(ConfigurationExpression config, ILifecycle lifecycle)
            {
                config.For<TInterface>()
                    .LifecycleIs(lifecycle)
                    .Use(context => context.GetInstance<TImplementor>());
            }
        }

    }
}

namespace NServiceBus
{
    using global::StructureMap;

    /// <summary>
    /// StructureMap extension to pass an existing StructureMap container instance.
    /// </summary>
    public static class StructureMapExtensions
    {
        /// <summary>
        /// Use the Spring passing in a pre-configured container to be used by NServiceBus.
        /// </summary>
        /// <param name="customizations"></param>
        /// <param name="container">The existing container instance.</param>
        public static void ExistingContainer(this ContainerCustomizations customizations, IContainer container)
        {
            customizations.Settings.Set("ExistingContainer", container);
        }
    }
}