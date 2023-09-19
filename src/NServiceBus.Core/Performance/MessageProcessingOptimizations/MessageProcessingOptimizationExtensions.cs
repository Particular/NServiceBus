namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// Configuration class for durable messaging.
    /// </summary>
    public static class MessageProcessingOptimizationExtensions
    {
        /// <summary>
        /// Instructs the transport to limits the allowed concurrency when processing messages.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="maxConcurrency">The max concurrency allowed.</param>
        public static void LimitMessageProcessingConcurrencyTo(this EndpointConfiguration config, int maxConcurrency)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);

            config.Settings.Get<ReceiveComponent.Settings>().PushRuntimeSettings = new PushRuntimeSettings(maxConcurrency);
        }
    }
}