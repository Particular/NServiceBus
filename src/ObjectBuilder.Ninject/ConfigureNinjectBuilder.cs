namespace NServiceBus
{
    using Ninject;
    using ObjectBuilder.Common.Config;
    using ObjectBuilder.Ninject;

    /// <summary>
    /// The static class which holds <see cref="NServiceBus"/> extensions methods.
    /// </summary>
    public static class ConfigureNinjectBuilder
    {
        /// <summary>
        /// Instructs <see cref="NServiceBus"/> to use the provided kernel
        /// </summary>
        /// <param name="config">The extended Configure.</param>
        /// <returns>The Configure.</returns>
        public static Configure NinjectBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new NinjectObjectBuilder());
            return config;
        }

        /// <summary>
        /// Instructs <see cref="NServiceBus"/> to use the provided kernel
        /// </summary>
        /// <param name="config">The extended Configure.</param>
        /// <param name="kernel">The kernel.</param>
        /// <returns>The Configure.</returns>
        public static Configure NinjectBuilder(this Configure config, IKernel kernel)
        {
            ConfigureCommon.With(config, new NinjectObjectBuilder(kernel));
            return config;
        }
    }
}