namespace NServiceBus
{
    using Autofac;
    using ObjectBuilder.Autofac;
    using ObjectBuilder.Common.Config;

    /// <summary>
    /// Contains extension methods to <see cref="Configure"/>.
    /// </summary>
    public static class ConfigureAutofacBuilder
    {
        /// <summary>
        /// Use the Autofac builder.
        /// </summary>
        /// <param name="config">The configuration context.</param>
        /// <returns>The configuration context.</returns>
        public static Configure AutofacBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new AutofacObjectBuilder());
            return config;
        }

        /// <summary>
        /// Use the Autofac builder passing in a pre-configured container to be used by nServiceBus.
        /// </summary>
        /// <param name="config">The configuration context.</param>
        /// <param name="rootScope">The root-most lifetime scope.</param>
        /// <returns>The configuration context.</returns>
        public static Configure AutofacBuilder(this Configure config, ILifetimeScope rootScope)
        {
            ConfigureCommon.With(config, new AutofacObjectBuilder(rootScope));
            return config;
        }
    }
}