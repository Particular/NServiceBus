using NServiceBus.ObjectBuilder.CastleWindsor;
using Castle.Windsor;

namespace NServiceBus
{
    using ObjectBuilder.Common.Config;

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

        /// <summary>
        /// Use the Castle Windsor builder passing in a preconfigured container to be used by nServiceBus.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static Configure CastleWindsorBuilder(this Configure config, IWindsorContainer container)
        {
            ConfigureCommon.With(config, new WindsorObjectBuilder(container));

            return config;
        } 
    }
}
