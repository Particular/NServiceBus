using NServiceBus.ObjectBuilder.CastleWindsor;
using NServiceBus.ObjectBuilder.Common.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureWindsorBuilder
    {
        /// <summary>
        /// Use the Castle Windsor builder.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure CastleWindsorBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new WindsorObjectBuilder());

            return config;
        }        
    }
}
