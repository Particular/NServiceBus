namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;

    /// <summary>
    /// Factory for creating new bus instances 
    /// </summary>
    public static class Bus
    {
        /// <summary>
        /// Creates a bus instance with the given configuration
        /// </summary>
        /// <param name="configuration">The configuration to use</param>
        /// <returns></returns>
        public static IStartableBus Create(BusConfiguration configuration)
        {
            Guard.AgainstNull(configuration, "configuration");
            var config = configuration.BuildConfiguration();

            config.Initialize();

            return config.Builder.Build<IStartableBus>();
        }

        /// <summary>
        /// Creates a bus instance to be used in send only mode
        /// </summary>
        /// <param name="configuration">The configuration to use</param>
        /// <returns></returns>
        public static ISendOnlyBus CreateSendOnly(BusConfiguration configuration)
        {
            Guard.AgainstNull(configuration, "configuration");
            configuration.GetSettings().Set("Endpoint.SendOnly", true);

            var config = configuration.BuildConfiguration();

            config.Initialize();

            return config.Builder.Build<ISendOnlyBus>();
        }
         
    }
}