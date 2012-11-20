using NServiceBus.ObjectBuilder.Autofac;
using Autofac;

namespace NServiceBus
{
    using ObjectBuilder.Common.Config;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureAutofacBuilder
    {
        /// <summary>
        /// Use the Autofac builder.
        /// </summary>
        /// <param name="config">The configuration context.</param>
        /// <returns>The configuration context.</returns>
        public static Configure AutofacBuilder(this Configure config)
        {
            return config.AutofacBuilder(null);
        }

        /// <summary>
        /// Use the Autofac builder passing in a preconfigured container to be used by nServiceBus.
        /// </summary>
        /// <param name="config">The configuration context.</param>
        /// <param name="container">The preconfigured container to be used.</param>
        /// <returns>The configuration context.</returns>
        public static Configure AutofacBuilder(this Configure config, IContainer container)
        {
            return config.AutofacBuilder(container as ILifetimeScope);
        }

        /// <summary>
        /// Use the Autofac builder passing in a preconfigured container to be used by nServiceBus.
        /// </summary>
        /// <param name="config">The configuration context.</param>
        /// <param name="rootScope">The root-most lifetime scope.</param>
        /// <returns>The configuration context.</returns>
        public static Configure AutofacBuilder(this Configure config, ILifetimeScope rootScope)
        {
            ConfigureCommon.With(config, new AutofacObjectBuilder(rootScope));
            return config;
        }
    }
}