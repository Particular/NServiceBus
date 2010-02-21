using System;
using NServiceBus.ObjectBuilder.Spring;
using NServiceBus.ObjectBuilder.Common.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for configuring the Spring container.
    /// </summary>
    public static class ConfigureSpringBuilder
    {
        /// <summary>
        /// Obsolete - use DefaultBuilder if you don't care which container is used.
        /// If you want to use the Spring Framework as your container, call <see cref="SpringFrameworkBuilder"/>.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [Obsolete]
        public static Configure SpringBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder());

            return config;
        }

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

    }
}
