namespace NServiceBus
{
    /// <summary>
    /// Indicate that the implementing class will specify configuration.
    /// </summary>
    public interface INeedInitialization
    {
        /// <summary>
        /// Allows to override default settings.
        /// </summary>
        /// <param name="builder">Endpoint configuration builder.</param>
        void Customize(ConfigurationBuilder builder);
    }
}