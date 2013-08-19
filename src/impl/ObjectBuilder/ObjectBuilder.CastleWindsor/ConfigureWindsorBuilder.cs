namespace NServiceBus
{
    using ObjectBuilder.CastleWindsor;
    using Castle.Windsor;
    using ObjectBuilder.Common.Config;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureWindsorBuilder
    {
        /// <summary>
        /// Use the Castle Windsor builder.
        /// </summary>
        public static Configure CastleWindsorBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new WindsorObjectBuilder());

            return config;
        }

        /// <summary>
        /// Use the Castle Windsor builder passing in a pre-configured container to be used by nServiceBus.
        /// </summary>
        public static Configure CastleWindsorBuilder(this Configure config, IWindsorContainer container)
        {
            ConfigureCommon.With(config, new WindsorObjectBuilder(container));

            return config;
        }
    }
}