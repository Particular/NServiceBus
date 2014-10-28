namespace NServiceBus
{
    using System;
    using ObjectBuilder.Common.Config;
    using ObjectBuilder.Spring;
    using Spring.Context.Support;

    /// <summary>
    /// Contains extension methods to <see cref="Configure"/> for configuring the Spring container.
    /// </summary>
    public static class ConfigureSpringBuilder
    {
        /// <summary>
        /// Use the Spring Framework as the container.
        /// </summary>
        public static Configure SpringFrameworkBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder());

            return config;
        }

        /// <summary>
        /// Use the Spring Framework as the container with the initialized application context
        /// </summary>
        [CLSCompliant(false)]
        public static Configure SpringFrameworkBuilder(this Configure config,GenericApplicationContext applicationContext)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder(applicationContext));

            return config;
        }

    }
}
