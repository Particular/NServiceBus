namespace NServiceBus.Pipeline
{
    using NServiceBus.Features;

    /// <summary>
    /// Allows configuring of the message processing feature.
    /// </summary>
    public static class MessageProcessingBusConfigurationExtensions
    {
        /// <summary>
        /// Disables the main processing pipeline (by removing all the standard pipeline behaviors). 
        /// </summary>
        public static void DisableMainProcessingPipeline(this BusConfiguration busConfiguration)
        {
            busConfiguration.DisableFeature<MessageProcessingFeature>();
            busConfiguration.DisableFeature<FirstLevelRetries>();
        }
    }
}