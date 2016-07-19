namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;

    /// <summary>
    /// Extension methods for recoverability which extend <see cref="EndpointConfiguration" />.
    /// </summary>
    public static class RecoverabilityEndpointConfigurationExtensions
    {
        /// <summary>
        /// Configuration settings for recoverability.
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        public static RecoverabilitySettings Recoverability(this EndpointConfiguration configuration)
        {
            return new RecoverabilitySettings(configuration.GetSettings());
        }
    }
}