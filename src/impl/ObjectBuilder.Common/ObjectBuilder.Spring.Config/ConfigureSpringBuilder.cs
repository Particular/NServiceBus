using System;
using NServiceBus.ObjectBuilder.Spring;
using NServiceBus.ObjectBuilder.Common.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for configuring the Spring container.
    /// </summary>
    public static class ConfigureSpringBuilder
    {
        /// <summary>
        /// Use the Spring Framework as the container.
        /// The given actions will be performed as a part of the initialization process.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure SpringBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder());

            return config;
        }
    }
}
