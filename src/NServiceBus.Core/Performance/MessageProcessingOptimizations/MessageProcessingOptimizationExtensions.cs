#nullable enable

namespace NServiceBus;

using System;
using Transport;

/// <summary>
/// Configuration class for durable messaging.
/// </summary>
public static class MessageProcessingOptimizationExtensions
{
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    extension(EndpointConfiguration config)
    {
        /// <summary>
        /// Instructs the transport to limits the allowed concurrency when processing messages.
        /// </summary>
        /// <param name="maxConcurrency">The max concurrency allowed.</param>
        public void LimitMessageProcessingConcurrencyTo(int maxConcurrency)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);

            config.Settings.Get<ReceiveComponent.Settings>().PushRuntimeSettings = new PushRuntimeSettings(maxConcurrency);
        }
    }
}