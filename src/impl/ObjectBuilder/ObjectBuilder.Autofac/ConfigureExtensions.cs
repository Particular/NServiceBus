using System;
using NServiceBus.ObjectBuilder;
using NServiceBus.ObjectBuilder.Autofac;
using NServiceBus.ObjectBuilder.Common.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureExtensions
    {
        /// <summary>
        /// Use the Autofac builder.
        /// 
        /// You can pass actions to be performed during initialization with the
        /// configured builder.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="configActions"></param>
        /// <returns></returns>
        public static Configure AutofacBuilder(this Configure config, params Action<IConfigureComponents>[] configActions)
        {
            ConfigureCommon.With(config, new AutofacObjectBuilder(), configActions);

            return config;
        }
    }
}
