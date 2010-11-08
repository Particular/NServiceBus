using NServiceBus.ObjectBuilder.Autofac;
using NServiceBus.ObjectBuilder.Common.Config;
using Autofac;

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
        public static Configure AutofacBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new AutofacObjectBuilder());

            return config;
        }

        /// <summary>
        /// Use the Autofac builder passing in a preconfigured container to be used by nServiceBus.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static Configure AutofacBuilder(this Configure config, IContainer container)
        {
            ConfigureCommon.With(config, new AutofacObjectBuilder(container));
            return config;
        }
    }
}
