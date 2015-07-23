namespace NServiceBus
{
    using System;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.Extensibility;

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
            Guard.AgainstNull(options, "options");
            Guard.AgainstNegative(delay, "delay");

            options.GetExtensions().Set(new ApplyDelayedDeliveryConstraintBehavior.State(new DelayDeliveryWith(delay)));
        }
        /// <summary>
        /// Requests that the message should not be delivered before the specified time.
        /// </summary>
        /// <param name="options">The options being extended.</param>
        /// <param name="at">The time when this message should be made available.</param>
        public static void DoNotDeliverBefore(this SendOptions options, DateTime at)
        {
            Guard.AgainstNull(options, "options");

            options.GetExtensions().Set(new ApplyDelayedDeliveryConstraintBehavior.State(new DoNotDeliverBefore(at)));
        }
    }
}