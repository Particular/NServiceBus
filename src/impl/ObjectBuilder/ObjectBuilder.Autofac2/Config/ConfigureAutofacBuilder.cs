using Autofac;
using NServiceBus.ObjectBuilder.Autofac2;
using NServiceBus.ObjectBuilder.Common.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureAutofacBuilder
    {
        /// <summary>
        /// Use the Autofac builder.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure Autofac2Builder(this Configure config)
        {
            ConfigureCommon.With(config, new Autofac2ObjectBuilder());

            return config;
        }

        /// <summary>
        /// Use the Autofac builder passing in a preconfigured container to be used by nServiceBus.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static Configure Autofac2Builder(this Configure config, IContainer container)
        {
            ConfigureCommon.With(config, new Autofac2ObjectBuilder(container));
            return config;
        }
    }
}