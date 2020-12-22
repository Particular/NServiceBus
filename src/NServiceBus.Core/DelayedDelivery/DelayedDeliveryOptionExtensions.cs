namespace NServiceBus
{
    using System;
    using DelayedDelivery;

    /// <summary>
    /// Provides ways for the end user to request delayed delivery of their messages.
    /// </summary>
    public static class DelayedDeliveryOptionExtensions
    {
        /// <summary>
        /// Delays the delivery of the message with the specified delay.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        /// <param name="delay">The requested delay.</param>
        public static void DelayDeliveryWith(this SendOptions options, TimeSpan delay)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNegative(nameof(delay), delay);

            if (options.TransportProperties.DoNotDeliverBefore != null)
            {
                throw new InvalidOperationException($"The options are already configured for delayed delivery by the '{nameof(DoNotDeliverBefore)}' API.");
            }

            options.TransportProperties.DelayDeliveryWith = new DelayDeliveryWith(delay);
        }

        /// <summary>
        /// Returns the configured delivery delay by using <see cref="DelayDeliveryWith" />.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        /// <returns>The configured <see cref="TimeSpan" /> or <c>null</c>.</returns>
        public static TimeSpan? GetDeliveryDelay(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            return options.TransportProperties?.DelayDeliveryWith?.Delay;
        }

        /// <summary>
        /// Requests that the message should not be delivered before the specified time.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        /// <param name="at">The time when this message should be made available.</param>
        public static void DoNotDeliverBefore(this SendOptions options, DateTimeOffset at)
        {
            Guard.AgainstNull(nameof(options), options);

            if (options.TransportProperties.DelayDeliveryWith != null)
            {
                throw new InvalidOperationException($"The options are already configured for delayed delivery by the '{nameof(DelayDeliveryWith)}' API.");
            }

            options.TransportProperties.DoNotDeliverBefore = new DoNotDeliverBefore(at);
        }

        /// <summary>
        /// Returns the delivery date configured by using <see cref="DoNotDeliverBefore" />.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        /// <returns>The configured <see cref="DateTimeOffset" /> or <c>null</c>.</returns>
        public static DateTimeOffset? GetDeliveryDate(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            return options.TransportProperties?.DoNotDeliverBefore?.At;
        }
    }
}