namespace NServiceBus
{
    using System;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;

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

            options.GetExtensions().AddDeliveryConstraint(new DelayDeliveryWith(delay));
        }

        /// <summary>
        /// Returns the configured delivery delay by using <see cref="DelayDeliveryWith" />.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        /// <returns>The configured <see cref="TimeSpan" /> or <c>null</c>.</returns>
        public static TimeSpan? GetDeliveryDelay(this SendOptions options)
        {
            DelayDeliveryWith delay;
            options.GetExtensions().TryGetDeliveryConstraint(out delay);

            return delay?.Delay;
        }

        /// <summary>
        /// Requests that the message should not be delivered before the specified time.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        /// <param name="at">The time when this message should be made available.</param>
        public static void DoNotDeliverBefore(this SendOptions options, DateTimeOffset at)
        {
            Guard.AgainstNull(nameof(options), options);

            options.GetExtensions().AddDeliveryConstraint(new DoNotDeliverBefore(at.UtcDateTime));
        }

        /// <summary>
        /// Returns the delivery date configured by using <see cref="DoNotDeliverBefore" />.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        /// <returns>The configured <see cref="DateTimeOffset" /> or <c>null</c>.</returns>
        public static DateTimeOffset? GetDeliveryDate(this SendOptions options)
        {
            DoNotDeliverBefore deliveryDate;
            options.GetExtensions().TryGetDeliveryConstraint(out deliveryDate);

            return deliveryDate?.At;
        }
    }
}