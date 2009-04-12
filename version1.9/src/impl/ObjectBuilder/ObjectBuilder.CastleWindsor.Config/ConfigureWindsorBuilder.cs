using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.CastleWindsor;
using NServiceBus.ObjectBuilder.Common.Config;
using Castle.Windsor;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureWindsorBuilder
    {
        /// <summary>
        /// Use the Castle Windsor builder.
        /// 
        /// You can pass actions to be performed during initialization with the
        /// configured builder.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="configActions"></param>
        /// <returns></returns>
        public static Configure CastleWindsorBuilder(this Configure config, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new WindsorObjectBuilder(), configActions);

            return config;
        }

        /// <summary>
        /// Use the Castle Windsor builder, passing in your own container.
        /// 
        /// You can pass actions to be performed during initialization with the
        /// configured builder.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="container"></param>
        /// <param name="configActions"></param>
        /// <returns></returns>
        public static Configure CastleWindsorBuilder(this Configure config, IWindsorContainer container, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new WindsorObjectBuilder(container), configActions);

            return config;
        }
        
    }
}
