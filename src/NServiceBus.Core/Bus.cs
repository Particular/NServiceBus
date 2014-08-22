namespace NServiceBus
{
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
            var config = configuration.BuildConfiguration();

            config.Initialize();

            return config.Builder.Build<IStartableBus>();
        }
         
    }
}