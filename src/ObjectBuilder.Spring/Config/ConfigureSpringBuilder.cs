using NServiceBus.ObjectBuilder.Spring;
using Spring.Context.Support;

namespace NServiceBus
{
    using ObjectBuilder.Common.Config;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for configuring the Spring container.
    /// </summary>
    public static class ConfigureSpringBuilder
    {
        /// <summary>
        /// Use the Spring Framework as the container.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure SpringFrameworkBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder());

            return config;
        }

        /// <summary>
        /// Use the Spring Framework as the container with the inilialized application context
        /// </summary>
        /// <param name="config"></param>
        /// <param name="applicationContext"></param>
        /// <returns></returns>
        public static Configure SpringFrameworkBuilder(this Configure config,GenericApplicationContext applicationContext)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder(applicationContext));

            return config;
        }

        

    }
}
