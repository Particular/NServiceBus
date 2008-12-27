using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder.Spring;
using NServiceBus.ObjectBuilder.Common.Config;
using NServiceBus.ObjectBuilder;
using Spring.Context.Support;

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
        /// <param name="configActions"></param>
        /// <returns></returns>
        public static Configure SpringBuilder(this Configure config, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder(), configActions);

            return config;
        }

        /// <summary>
        /// Use the Spring Framework as the container with your own GenericApplicationContext.
        /// The given actions will be performed as a part of the initialization process.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="container"></param>
        /// <param name="configActions"></param>
        /// <returns></returns>
        public static Configure SpringBuilder(this Configure config, GenericApplicationContext container, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new SpringObjectBuilder(container), configActions);

            return config;
        }
    }
}
